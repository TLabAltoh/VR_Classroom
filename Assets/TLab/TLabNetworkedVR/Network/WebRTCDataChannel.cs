using System.Collections.Generic;
using System.Collections;
using Unity.WebRTC;
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;

namespace TLab.Network.WebRTC
{
    public class WebRTCDataChannel : MonoBehaviour
    {
        [Header("Signaling Server Address")]
        [SerializeField] private string m_serverAddr = "ws://localhost:3001";

        [Header("Connection State (Debug)")]
        [SerializeField] private string m_userID;
        [SerializeField] private string m_roomID;

        [Header("On Message Callback")]
        [SerializeField] private UnityEvent<string, string, byte[]> m_onMessage;

        // session dictionary
        private Dictionary<string, RTCPeerConnection> m_peerConnectionDic = new Dictionary<string, RTCPeerConnection>();
        private Dictionary<string, RTCDataChannel> m_dataChannelDic = new Dictionary<string, RTCDataChannel>();
        private Dictionary<string, bool> m_dataChannelFlagDic = new Dictionary<string, bool>();
        private Dictionary<string, List<RTCIceCandidate>> m_candidateDic = new Dictionary<string, List<RTCIceCandidate>>();

        // websocket instance
        private WebSocket m_websocket;

        // datachannle option
        private bool? m_orderd;
        private int? m_maxPacketLifeTime;
        private int? m_maxRetransmits;
        private string m_protocol;
        private bool? m_negotiated;
        private int? m_id;

        // this class name
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public int eventCount => m_onMessage.GetPersistentEventCount();

        public void SetSignalingServerAddr(string addr) => m_serverAddr = addr;

        public void SetCallback(UnityAction<string, string, byte[]> callback)
        {
            m_onMessage.RemoveAllListeners();
            m_onMessage.AddListener(callback);
        }

        #region ICE_CANDIDATE
        private void AddIceCandidate(string src, RTCICE tlabIce)
        {
            var candidateInfo = new RTCIceCandidateInit();
            candidateInfo.sdpMLineIndex = tlabIce.sdpMLineIndex.TryToInt();
            candidateInfo.sdpMid = tlabIce.sdpMid;
            candidateInfo.candidate = tlabIce.candidate;
            var candidate = new RTCIceCandidate(candidateInfo);

            if (!m_candidateDic.ContainsKey(src))
            {
                m_candidateDic[src] = new List<RTCIceCandidate>();
            }

            m_candidateDic[src].Add(candidate);

            if (m_peerConnectionDic.ContainsKey(src))
            {
                foreach (RTCIceCandidate tmp in m_candidateDic[src])
                {
                    m_peerConnectionDic[src].AddIceCandidate(tmp);
                    Debug.Log(THIS_NAME + "add ice candidate");
                }
                m_candidateDic[src].Clear();
            }
        }

        private void SendIceCandidate(string dst, RTCIceCandidate candidate)
        {
            RTCICE tlabICE = new RTCICE();
            tlabICE.sdpMLineIndex = candidate.SdpMLineIndex.ToJson();
            tlabICE.sdpMid = candidate.SdpMid;
            tlabICE.candidate = candidate.Candidate;

            SendWsMeg(RTCSigAction.ICE, null, tlabICE, dst);
        }

        private void OnIceConnectionChange(RTCIceConnectionState state)
        {
            switch (state)
            {
                case RTCIceConnectionState.New:
                    Debug.Log(THIS_NAME + "IceConnectionState: New");
                    break;
                case RTCIceConnectionState.Checking:
                    Debug.Log(THIS_NAME + "IceConnectionState: Checking");
                    break;
                case RTCIceConnectionState.Closed:
                    Debug.Log(THIS_NAME + "IceConnectionState: Closed");
                    break;
                case RTCIceConnectionState.Completed:
                    Debug.Log(THIS_NAME + "IceConnectionState: Completed");
                    break;
                case RTCIceConnectionState.Connected:
                    Debug.Log(THIS_NAME + "IceConnectionState: Connected");
                    break;
                case RTCIceConnectionState.Disconnected:
                    Debug.Log(THIS_NAME + "IceConnectionState: Disconnected");
                    break;
                case RTCIceConnectionState.Failed:
                    Debug.Log(THIS_NAME + "IceConnectionState: Failed");
                    break;
                case RTCIceConnectionState.Max:
                    Debug.Log(THIS_NAME + "IceConnectionState: Max");
                    break;
                default:
                    break;
            }
        }

        private void OnIceCandidate(string dst, RTCIceCandidate candidate)
        {
            SendIceCandidate(dst, candidate);

            Debug.Log(THIS_NAME + $"ICE candidate:\n {candidate.Candidate}");
        }
        #endregion ICE_CANDIDATE

        #region SESSION_DESCRIPTION
        private void OnSetLocalSuccess(RTCPeerConnection pc)
        {
            Debug.Log(THIS_NAME + $"SetLocalDescription complete");
        }

        private void OnSetRemoteSuccess(RTCPeerConnection pc)
        {
            Debug.Log(THIS_NAME + $"SetRemoteDescription complete");
        }

        private void OnCreateSessionDescriptionError(RTCError e) { }

        private void OnSetSessionDescriptionError(ref RTCError error) { }
        #endregion SESSION_DESCRIPTION

        #region SIGNALING
        private void CreatePeerConnection(string dst, bool call)
        {
            Debug.Log(THIS_NAME + "create new peerConnection start");

            RTCConfiguration configuration = GetSelectedSdpSemantics();
            m_peerConnectionDic[dst] = new RTCPeerConnection(ref configuration);
            m_peerConnectionDic[dst].OnIceCandidate = candidate => { OnIceCandidate(dst, candidate); };
            m_peerConnectionDic[dst].OnIceConnectionChange = state => { OnIceConnectionChange(state); };

            if (call == true)
            {
                Debug.Log(THIS_NAME + "create new dataChennel start");

                RTCDataChannelInit conf = new RTCDataChannelInit
                {
                    protocol = m_protocol,
                    ordered = m_orderd,
                    negotiated = m_negotiated,
                    maxPacketLifeTime = m_maxPacketLifeTime,
                    maxRetransmits = m_maxRetransmits,
                    id = m_id
                };

                m_dataChannelDic[dst] = m_peerConnectionDic[dst].CreateDataChannel("data", conf);
                m_dataChannelDic[dst].OnMessage = bytes => { m_onMessage.Invoke(m_userID, dst, bytes); };
                m_dataChannelDic[dst].OnOpen = () => {
                    Debug.Log(THIS_NAME + dst + ": DataChannel Open");
                    m_dataChannelFlagDic[dst] = true;
                };
                m_dataChannelDic[dst].OnClose = () => {
                    Debug.Log(THIS_NAME + dst + ": DataChannel Close");
                };
                m_dataChannelFlagDic[dst] = false;
            }
            else
            {
                m_peerConnectionDic[dst].OnDataChannel = channel =>
                {
                    Debug.Log(THIS_NAME + "dataChannel created on offer peerConnection");

                    m_dataChannelFlagDic[dst] = true;
                    m_dataChannelDic[dst] = channel;
                    m_dataChannelDic[dst].OnMessage = bytes => { m_onMessage.Invoke(m_userID, dst, bytes); };
                    m_dataChannelDic[dst].OnClose = () => {
                        Debug.Log(THIS_NAME + dst + ": DataChannel Close");
                    };
                };
            }
        }

        private RTCConfiguration GetSelectedSdpSemantics()
        {
            RTCConfiguration config = default;
            config.iceServers = new RTCIceServer[]
            {
            new RTCIceServer { urls = new string[] { "stun:stun.l.google.com:19302" } }
            };

            return config;
        }

        private RTCDesc GetRTCDesc(RTCSessionDescription desc)
        {
            RTCDesc tlabDesc = new RTCDesc();
            tlabDesc.type = (int)desc.type;
            tlabDesc.sdp = desc.sdp;

            return tlabDesc;
        }

        private IEnumerator OnCreateAnswerSuccess(string dst, RTCSessionDescription desc)
        {
            Debug.Log(THIS_NAME + $"create answer success:\n{desc.sdp}");

            Debug.Log(THIS_NAME + "peerConnection.setLocalDescription start");

            var op = m_peerConnectionDic[dst].SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess(m_peerConnectionDic[dst]);
            }
            else
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }

            Debug.Log(THIS_NAME + "peerConnection send local description start");

            SendWsMeg(RTCSigAction.ANSWER, GetRTCDesc(desc), null, dst);
        }

        private IEnumerator OnCreateOfferSuccess(string dst, RTCSessionDescription desc)
        {
            Debug.Log(THIS_NAME + $"Offer from pc\n{desc.sdp}");

            Debug.Log(THIS_NAME + "pc setLocalDescription start");
            var op = m_peerConnectionDic[dst].SetLocalDescription(ref desc);
            yield return op;

            if (!op.IsError)
            {
                OnSetLocalSuccess(m_peerConnectionDic[dst]);
            }
            else
            {
                var error = op.Error;
                OnSetSessionDescriptionError(ref error);
            }

            Debug.Log(THIS_NAME + "pc send local description start");

            SendWsMeg(RTCSigAction.OFFER, GetRTCDesc(desc), null, dst);
        }

        private IEnumerator OnAnswer(string src, RTCSessionDescription desc)
        {
            Debug.Log(THIS_NAME + "peerConnection.setRemoteDescription start");

            var op2 = m_peerConnectionDic[src].SetRemoteDescription(ref desc);
            yield return op2;
            if (!op2.IsError)
            {
                OnSetRemoteSuccess(m_peerConnectionDic[src]);
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }

            yield break;
        }

        private IEnumerator OnOffer(string src, RTCSessionDescription desc)
        {
            CreatePeerConnection(src, false);

            Debug.Log(THIS_NAME + "peerConnection.setRemoteDescription start");

            var op2 = m_peerConnectionDic[src].SetRemoteDescription(ref desc);
            yield return op2;

            if (!op2.IsError)
            {
                OnSetRemoteSuccess(m_peerConnectionDic[src]);
            }
            else
            {
                var error = op2.Error;
                OnSetSessionDescriptionError(ref error);
            }

            Debug.Log(THIS_NAME + "peerConnection.createAnswer start");

            var op3 = m_peerConnectionDic[src].CreateAnswer();
            yield return op3;

            if (!op3.IsError)
            {
                yield return StartCoroutine(OnCreateAnswerSuccess(src, op3.Desc));
            }
            else
            {
                OnCreateSessionDescriptionError(op3.Error);
            }

            yield break;
        }

        private IEnumerator Call(string dst)
        {
            if (m_dataChannelDic.ContainsKey(dst) || m_dataChannelFlagDic.ContainsKey(dst) || m_peerConnectionDic.ContainsKey(dst))
            {
                Debug.LogError(THIS_NAME + "dst is already exist");
                yield break;
            }

            CreatePeerConnection(dst, true);

            Debug.Log(THIS_NAME + "pc createOffer start");
            var op = m_peerConnectionDic[dst].CreateOffer();
            yield return op;

            if (!op.IsError)
            {
                yield return StartCoroutine(OnCreateOfferSuccess(dst, op.Desc));
            }
            else
            {
                OnCreateSessionDescriptionError(op.Error);
            }
        }
        #endregion SIGNALING

        #region UTILITY
        private RTCSessionDescription GetDescription(RTCDesc tlabDesc)
        {
            RTCSessionDescription result = new RTCSessionDescription();
            result.type = (RTCSdpType)tlabDesc.type;
            result.sdp = tlabDesc.sdp;
            return result;
        }

        public void Join(string userID, string roomID,
                         string protocol = null, bool? orderd = null, bool? negotiated = null,
                         int? maxPacketLifeTime = null, int? maxRetransmits = null, int? id = null)
        {
            // User info for signaling server
            this.m_userID = userID;
            this.m_roomID = roomID;

            // Datachannel option
            m_protocol = protocol;
            m_orderd = orderd;
            m_negotiated = negotiated;
            m_maxPacketLifeTime = maxPacketLifeTime;
            m_maxRetransmits = maxRetransmits;
            m_id = id;

            SendWsMeg(RTCSigAction.JOIN, null, null, null);
        }

        public void HangUp(string src)
        {
            // Close datachannel befor offer
            if (m_dataChannelDic.ContainsKey(src) == true)
            {
                m_dataChannelDic[src].Close();
                m_dataChannelDic[src] = null;
                m_dataChannelDic.Remove(src);

                Debug.Log(THIS_NAME + "hung up datachannel: " + src);
            }

            // Datachannel flag delete
            if (m_dataChannelFlagDic.ContainsKey(src) == true)
            {
                m_dataChannelFlagDic.Remove(src);

                Debug.Log(THIS_NAME + "remove datachannle flag: " + src);
            }

            // Close datachannel befor offer
            if (m_peerConnectionDic.ContainsKey(src) == true)
            {
                m_peerConnectionDic[src].Close();
                m_peerConnectionDic[src] = null;
                m_peerConnectionDic.Remove(src);

                Debug.Log(THIS_NAME + "remove peerconnection: " + src);
            }
        }

        public void HangUpAll()
        {
            // https://dobon.net/vb/dotnet/programing/dictionarytoarray.html
            // https://ja.stackoverflow.com/questions/10119/foreach%E6%96%87%E3%81%A7%E4%B8%AD%E8%BA%AB%E3%81%AE%E5%87%A6%E7%90%86%E4%B8%AD%E3%81%AB%E6%AF%8D%E9%9B%86%E5%90%88%E5%81%B4%E3%81%8C%E5%A4%89%E5%8C%96%E3%81%99%E3%82%8B%E3%81%A8movenext%E3%81%A7%E3%82%A8%E3%83%A9%E3%83%BC%E3%81%AB%E3%81%AA%E3%82%8B

            // Close datachannel befor offer
            if (m_dataChannelDic.Count > 0)
            {
                List<string> dsts = new List<string>(m_dataChannelDic.Keys);
                foreach (string dst in dsts)
                {
                    m_dataChannelDic[dst].Close();
                    m_dataChannelDic[dst] = null;
                    m_dataChannelDic.Remove(dst);
                    Debug.Log(THIS_NAME + "hung up datachannel: " + dst);
                }
            }

            // Datachannel flag delete
            if (m_dataChannelFlagDic.Count > 0)
            {
                List<string> dsts = new List<string>(m_dataChannelFlagDic.Keys);
                foreach (string dst in dsts)
                {
                    m_dataChannelFlagDic.Remove(dst);
                    Debug.Log(THIS_NAME + "remove datachannle flag: " + dst);
                }
            }

            // Close datachannel befor offer
            if (m_peerConnectionDic.Count > 0)
            {
                List<string> dsts = new List<string>(m_peerConnectionDic.Keys);
                foreach (string dst in dsts)
                {
                    m_peerConnectionDic[dst].Close();
                    m_peerConnectionDic[dst] = null;
                    m_peerConnectionDic.Remove(dst);
                    Debug.Log(THIS_NAME + "remove peerconnection: " + dst);
                }
            }
        }

        public void Exit()
        {
            HangUpAll();

            SendWsMeg(RTCSigAction.EXIT, null, null, null);

            this.m_userID = "";
            this.m_roomID = "";
        }

        public void SendRTCMsg(byte[] bytes)
        {
            foreach (string id in m_dataChannelDic.Keys)
            {
                RTCDataChannel dataChannel = m_dataChannelDic[id];
                if (m_dataChannelFlagDic[id])
                {
                    dataChannel.Send(bytes);
                }
            }
        }

        public async void SendWsMeg(RTCSigAction action, RTCDesc desc, RTCICE ice, string dst)
        {
            if (m_websocket != null && m_websocket.State == WebSocketState.Open)
            {
                var obj = new RTCSigJson();
                obj.src = m_userID;
                obj.room = m_roomID;
                obj.dst = dst;
                obj.action = (int)action;
                obj.desc = desc;
                obj.ice = ice;

                string json = JsonUtility.ToJson(obj);

                Debug.Log(THIS_NAME + "send ws message: " + json);

                await m_websocket.SendText(json);
            }
        }

        public void OnWsMsg(string message)
        {
            var parse = JsonUtility.FromJson<RTCSigJson>(message);

            switch (parse.action)
            {
                case (int)RTCSigAction.ICE:
                    AddIceCandidate(parse.src, parse.ice);
                    break;
                case (int)RTCSigAction.OFFER:
                    StartCoroutine(OnOffer(parse.src, GetDescription(parse.desc)));
                    break;
                case (int)RTCSigAction.ANSWER:
                    StartCoroutine(OnAnswer(parse.src, GetDescription(parse.desc)));
                    break;
                case (int)RTCSigAction.JOIN:
                    StartCoroutine(Call(parse.src));
                    break;
                case (int)RTCSigAction.EXIT:
                    HangUp(parse.src);
                    break;
            }
        }
        #endregion UTILITY

        private async IAsyncEnumerator<int> ConnectServerTask()
        {
            yield return -1;

            Debug.Log(THIS_NAME + "create call back start");
            Debug.Log(THIS_NAME + "connect to signaling server start");

            m_websocket = new WebSocket(m_serverAddr);

            m_websocket.OnOpen += () =>
            {
                Debug.Log(THIS_NAME + "Connection open!");
            };

            m_websocket.OnError += (e) =>
            {
                Debug.Log(THIS_NAME + "Error! " + e);
            };

            m_websocket.OnClose += (e) =>
            {
                Debug.Log(THIS_NAME + "Connection closed!");
            };

            m_websocket.OnMessage += (bytes) =>
            {
                string message = System.Text.Encoding.UTF8.GetString(bytes);

                Debug.Log(THIS_NAME + "OnWsMessage: " + message);

                OnWsMsg(message);
            };

            // waiting for messages
            await m_websocket.Connect();

            yield break;
        }

        private IEnumerator ConnectToSignalingServerStart()
        {
            // I don't know how many frames it takes to close the Websocket client.
            // So I'll wait for one frame anyway.

            yield return null;

            if (m_websocket != null)
            {
                m_websocket.Close();
                m_websocket = null;
            }

            yield return null;

            IAsyncEnumerator<int> task = ConnectServerTask();
            task.MoveNextAsync();

            yield return null;

            task.MoveNextAsync();

            yield break;
        }

        public void ConnectToSignalintServer()
        {
            StartCoroutine(ConnectToSignalingServerStart());
        }

        private void Start()
        {
            ConnectToSignalintServer();
        }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (m_websocket != null)
            {
                m_websocket.DispatchMessageQueue();
            }
#endif
        }

        private async void CloseWebSocket()
        {
            if (m_websocket != null)
            {
                await m_websocket.Close();
            }

            m_websocket = null;
        }

        void OnDestroy()
        {
            // OnDestroy()ÇÕCloseWebSocketÇÃèIóπÇë“ã@Ç∑ÇÈ ?
            CloseWebSocket();
        }

        void OnApplicationQuit()
        {
            CloseWebSocket();
        }
    }

    public static class TLabWebRTCExtensions
    {
        public static string ToJson(this int? value)
        {
            if (value == null) return null;
            return value.ToString();
        }

        public static int? TryToInt(this string value)
        {
            if (int.TryParse(value, out int result)) return result;
            return null;
        }
    }
}
