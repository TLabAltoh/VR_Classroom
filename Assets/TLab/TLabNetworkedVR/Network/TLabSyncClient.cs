using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;
using TLab.Network.WebRTC;

namespace TLab.XR.VRGrabber
{
    #region WebSocketUtil

    // https://kazupon.org/unity-jsonutility/#i-2
    [System.Serializable]
    public class WebVector3
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class WebVector4
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [System.Serializable]
    public class WebObjectInfo
    {
        public string id;
        public bool rigidbody;
        public bool gravity;
        public WebVector3 position;
        public WebVector4 rotation;
        public WebVector3 scale;
    }

    [System.Serializable]
    public class WebAnimInfo
    {
        public string id;
        public string parameter;
        public int type;

        public float floatVal;
        public int intVal;
        public bool boolVal;
        public string triggerVal;
    }

    [System.Serializable]
    public class TLabSyncJson
    {
        public int role;
        public int action;

        public int seatIndex = -1;

        public bool active = false;

        public WebObjectInfo transform;
        public WebAnimInfo animator;

        public int customIndex = -1;
        public string custom = "";
    }

    public enum WebRole
    {
        SERVER,
        HOST,
        GUEST
    }

    public enum WebAction
    {
        REGIST,
        REGECT,
        ACEPT,
        EXIT,
        GUESTDISCONNECT,
        GUESTPARTICIPATION,
        ALLOCATEGRAVITY,
        REGISTRBOBJ,
        GRABBLOCK,
        FORCERELEASE,
        DIVIDEGRABBER,
        SYNCTRANSFORM,
        SYNCANIM,
        CLEARTRANSFORM,
        CLEARANIM,
        REFLESH,
        UNIREFLESHTRANSFORM,
        UNIREFLESHANIM,
        CUSTOMACTION
    }

    public enum WebAnimValueType
    {
        TYPEFLOAT,
        TYPEINT,
        TYPEBOOL,
        TYPETRIGGER
    }

    public static class TLabSyncClientConst
    {
        // Top
        public const string COMMA = ",";
        public const string ROLE = "\"role\":";
        public const string ACTION = "\"action\":";
        public const string SEATINDEX = "\"seatIndex\":";
        public const string ACTIVE = "\"active\":";
        public const string TRANSFORM = "\"transform\":";
        public const string ANIMATOR = "\"animator\":";

        // Transform
        public const string TRANSFORM_ID = "\"id\":";
        public const string RIGIDBODY = "\"rigidbody\":";
        public const string GRAVITY = "\"gravity\":";
        public const string POSITION = "\"position\":";
        public const string ROTATION = "\"rotation\":";
        public const string SCALE = "\"scale\":";

        // Animator
        public const string ANIMATOR_ID = "\"id\":";
        public const string PARAMETER = "\"parameter\":";
        public const string TYPE = "\"type\":";

        public const string FLOATVAL = "\"floatVal\":";
        public const string INTVAL = "\"intVal\":";
        public const string BOOLVAL = "\"boolVal\":";
        public const string TRIGGERVAL = "\"triggerVal\":";

        // WebVector
        public const string X = "\"x\":";
        public const string Y = "\"y\":";
        public const string Z = "\"z\":";
        public const string W = "\"w\":";
    }

    #endregion WebSocketUtil

    [System.Serializable]
    public class TLabSyncClientCustomCallback
    {
        public void OnMessage(string message)
        {
            if (onMessage != null)
                onMessage.Invoke(message);
        }

        public void OnGuestParticipated(int seatIndex)
        {
            if (onGuestParticipated != null)
                onGuestParticipated.Invoke(seatIndex);
        }

        public void OnGuestDisconnected(int seatIndex)
        {
            if (onGuestDisconnected != null)
                onGuestDisconnected.Invoke(seatIndex);
        }

        [SerializeField] private UnityEvent<string> onMessage;
        [SerializeField] private UnityEvent<int> onGuestParticipated;
        [SerializeField] private UnityEvent<int> onGuestDisconnected;
    }

    [RequireComponent(typeof(TLabWebRTCDataChannel))]
    public class TLabSyncClient : MonoBehaviour
    {
        [Header("Server Info")]
        [Tooltip("Server Address (port 5000)")]
        [SerializeField] private string m_serverAddr = "ws://192.168.11.10:5000";

        [Tooltip("Your own avatar model (must be registered to enable synchronization)")]
        [Header("Own Avator")]
        [SerializeField] private GameObject m_cameraRig;
        [SerializeField] private GameObject m_rightHand;
        [SerializeField] private GameObject m_leftHand;
        [SerializeField] private Transform m_rootTransform;

        [Tooltip("The avatar model of the other party as seen from you")]
        [Header("Guest Avator")]
        [SerializeField] private GameObject m_guestHead;
        [SerializeField] private GameObject m_guestRTouch;
        [SerializeField] private GameObject m_guestLTouch;

        [Tooltip("Responce position of each player")]
        [Header("Respown Anchor")]
        [SerializeField] private Transform m_hostAnchor;
        [SerializeField] private Transform[] m_guestAnchors;

        [Tooltip("WebRTCDatachannel for synchronizing Transforms between players")]
        [Header("WebRTCDataChannel")]
        [SerializeField] private TLabWebRTCDataChannel dataChannel;

        [Tooltip("Custom message callbacks")]
        [Header("Custom Event")]
        [SerializeField] private TLabSyncClientCustomCallback[] m_customCallbacks;

        [Tooltip("Whether the user is Host")]
        [Header("User role")]
        [SerializeField] private bool m_isHost = false;

        [System.NonSerialized] public static TLabSyncClient Instalce;

        private WebSocket websocket;

        private const int SEAT_LENGTH = 5;
        private int m_seatIndex = -1;
        private bool[] m_guestTable = new bool[SEAT_LENGTH];


        private Hashtable m_grabbables = new Hashtable();
        private Hashtable m_animators = new Hashtable();

        private const string prefabName = "OVRGuestAnchor.";
        private const string thisName = "[tlabsyncclient] ";

        private delegate void ReceiveCallback(TLabSyncJson obj);

        ReceiveCallback[] receiveCallbacks = new ReceiveCallback[(int)WebAction.CUSTOMACTION + 1];

        public Hashtable Grabbables
        {
            get
            {
                return m_grabbables;
            }
        }

        public Hashtable Animators
        {
            get
            {
                return m_animators;
            }
        }

        public int SeatIndex
        {
            get
            {
                return m_seatIndex;
            }
        }

        public int SeatLength
        {
            get
            {
                return SEAT_LENGTH;
            }
        }

        public bool SocketIsOpen
        {
            get
            {
                return websocket == null ? false : websocket.State == WebSocketState.Open;
            }
        }

        public bool SocketIsConnecting
        {
            get
            {
                return websocket == null ? false : websocket.State == WebSocketState.Connecting;
            }
        }

        public bool IsHost
        {
            get
            {
                return m_isHost;
            }

            set
            {
                m_isHost = value;
            }
        }

        public void SetServerAddr(string addr)
        {
            m_serverAddr = addr;
        }

        public bool IsGuestExist(int index)
        {
            return m_guestTable[index];
        }

        #region SyncTargetUtility
        public void RemoveAllGrabbers()
        {
            foreach (DictionaryEntry entry in m_grabbables)
            {
                TLabSyncGrabbable grabbable = entry.Value as TLabSyncGrabbable;
                grabbable.ShutdownGrabber(false);
            }

            m_grabbables.Clear();
        }

        public void RemoveAllAnimators()
        {
            foreach (DictionaryEntry entry in m_animators)
            {
                TLabSyncAnim animator = entry.Value as TLabSyncAnim;
                animator.ShutdownAnimator(false);
            }

            m_animators.Clear();
        }

        public void AddSyncGrabbable(string name, TLabSyncGrabbable grabbable)
        {
            m_grabbables[name] = grabbable;
        }

        public void AddSyncAnimator(string name, TLabSyncAnim syncAnim)
        {
            m_animators[name] = syncAnim;
        }

        public void RemoveGrabber(string name)
        {
            m_grabbables.Remove(name);
        }

        public void RemoveAnimator(string name)
        {
            m_animators.Remove(name);
        }
        #endregion SyncTargetUtility

        #region Reflesh
        public void ForceReflesh(bool reloadWorldData)
        {
            TLabSyncJson obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.REFLESH,
                active = reloadWorldData
            };
            string json = JsonUtility.ToJson(obj);
            SendWsMessage(json);

            Debug.Log(thisName + "force reflesh");
        }

        public void UniReflesh(string targetName)
        {
            TLabSyncJson obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.UNIREFLESHTRANSFORM,
                transform = new WebObjectInfo
                {
                    id = targetName
                }
            };
            string json = JsonUtility.ToJson(obj);
            SendWsMessage(json);

            Debug.Log(thisName + "reflesh " + targetName);
        }
        #endregion Reflesh

        #region ConnectServer
        public void Exit()
        {
            string json =
                "{" +
                    TLabSyncClientConst.ROLE + (m_isHost ? ((int)WebRole.HOST).ToString() : ((int)WebRole.GUEST).ToString()) + TLabSyncClientConst.COMMA +
                    TLabSyncClientConst.ACTION + ((int)WebAction.EXIT).ToString() + TLabSyncClientConst.COMMA +
                    TLabSyncClientConst.SEATINDEX + (m_seatIndex.ToString()) +
                "}";

            SendWsMessage(json);
        }

        private async IAsyncEnumerator<int> ConnectServerTask()
        {
            yield return -1;

            receiveCallbacks[(int)WebAction.REGIST] = (obj) => { };
            receiveCallbacks[(int)WebAction.REGECT] = (obj) => { };
            receiveCallbacks[(int)WebAction.ACEPT] = (obj) => {
                #region
                m_seatIndex = obj.seatIndex;

                m_guestTable[obj.seatIndex] = true;

                // Enable sync own avator

                if (m_leftHand != null && m_rightHand != null && m_cameraRig != null)
                {
                    string guestName = prefabName + obj.seatIndex.ToString();

                    m_rightHand.name = guestName + ".RTouch";
                    m_leftHand.name = guestName + ".LTouch";
                    m_cameraRig.name = guestName + ".Head";

                    m_cameraRig.transform.localPosition = Vector3.zero;
                    m_cameraRig.transform.localRotation = Quaternion.identity;

                    if (m_seatIndex == 0)
                    {
                        m_rootTransform.position = m_hostAnchor.position;
                        m_rootTransform.rotation = m_hostAnchor.rotation;
                    }
                    else
                    {
                        Transform anchor = m_guestAnchors[m_seatIndex - 1];
                        m_rootTransform.position = anchor.position;
                        m_rootTransform.rotation = anchor.rotation;
                    }

                    m_rightHand.GetComponent<TLabSyncGrabbable>().m_enableSync = true;
                    m_leftHand.GetComponent<TLabSyncGrabbable>().m_enableSync = true;
                    m_cameraRig.GetComponent<TLabSyncGrabbable>().m_enableSync = true;
                }

                // TAdd TLabSyncGrabbable to hash table for fast lookup by name

                TLabSyncGrabbable[] grabbables = FindObjectsOfType<TLabSyncGrabbable>();
                foreach (TLabSyncGrabbable grabbable in grabbables)
                    m_grabbables[grabbable.gameObject.name] = grabbable;

                // Add animators to a hash table for fast lookup by name

                TLabSyncAnim[] syncAnims = FindObjectsOfType<TLabSyncAnim>();
                foreach (TLabSyncAnim syncAnim in syncAnims)
                    m_animators[syncAnim.gameObject.name] = syncAnim;

                // Connect to signaling server
                dataChannel.Join(this.gameObject.name + "_" + m_seatIndex.ToString(), "VR_Class");

                return;
                #endregion
            };
            receiveCallbacks[(int)WebAction.EXIT] = (obj) => { };
            receiveCallbacks[(int)WebAction.GUESTDISCONNECT] = (obj) => {
                #region
                if (m_guestTable[obj.seatIndex] == false) return;

                string guestName = prefabName + obj.seatIndex.ToString();

                GameObject guestRTouch = GameObject.Find(guestName + ".RTouch");
                GameObject guestLTouch = GameObject.Find(guestName + ".LTouch");
                GameObject guestHead = GameObject.Find(guestName + ".Head");

                if (guestRTouch != null)
                {
                    m_grabbables.Remove(guestRTouch.name);
                    UnityEngine.GameObject.Destroy(guestRTouch);
                }

                if (guestLTouch != null)
                {
                    m_grabbables.Remove(guestLTouch.name);
                    UnityEngine.GameObject.Destroy(guestLTouch);
                }

                if (guestHead != null)
                {
                    m_grabbables.Remove(guestHead.name);
                    UnityEngine.GameObject.Destroy(guestHead);
                }

                m_guestTable[obj.seatIndex] = false;

                foreach (TLabSyncClientCustomCallback callback in m_customCallbacks) callback.OnGuestDisconnected(obj.seatIndex);

                Debug.Log(thisName + "guest disconncted . " + obj.seatIndex.ToString());

                return;
                #endregion
            };
            receiveCallbacks[(int)WebAction.GUESTPARTICIPATION] = (obj) => {
                #region

                if (m_guestTable[obj.seatIndex] == true) return;

                Vector3 respownPos = new Vector3(0.0f, -0.5f, 0.0f);
                Quaternion respownRot = Quaternion.identity;

                string guestName = prefabName + obj.seatIndex.ToString();

                // Visualize avatars of newly joined players

                if (m_guestRTouch != null)
                {
                    GameObject guestRTouch = Instantiate(m_guestRTouch, respownPos, respownRot);
                    guestRTouch.name = guestName + ".RTouch";

                    m_grabbables[guestRTouch.name] = guestRTouch.GetComponent<TLabSyncGrabbable>();
                }

                if (m_guestLTouch != null)
                {
                    GameObject guestLTouch = Instantiate(m_guestLTouch, respownPos, respownRot);
                    guestLTouch.name = guestName + ".LTouch";

                    m_grabbables[guestLTouch.name] = guestLTouch.GetComponent<TLabSyncGrabbable>();
                }

                if (m_guestHead != null)
                {
                    GameObject guestHead = Instantiate(m_guestHead, respownPos, respownRot);
                    guestHead.name = guestName + ".Head";

                    m_grabbables[guestHead.name] = guestHead.GetComponent<TLabSyncGrabbable>();
                }

                m_guestTable[obj.seatIndex] = true;

                // 参加時のコールバック
                foreach (TLabSyncClientCustomCallback callback in m_customCallbacks) callback.OnGuestParticipated(obj.seatIndex);

                Debug.Log(thisName + "guest participated . " + obj.seatIndex.ToString());

                return;
                #endregion
            };
            receiveCallbacks[(int)WebAction.ALLOCATEGRAVITY] = (obj) => {
                #region
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = m_grabbables[webTransform.id] as TLabSyncGrabbable;
                if (grabbable != null) grabbable.AllocateGravity(obj.active);

                return;
                #endregion
            };
            receiveCallbacks[(int)WebAction.REGISTRBOBJ] = (obj) => { };
            receiveCallbacks[(int)WebAction.GRABBLOCK] = (obj) => {
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = m_grabbables[webTransform.id] as TLabSyncGrabbable;

                if (grabbable == null) return;

                grabbable.GrabbLockFromOutside(obj.seatIndex);

                return;
            };
            receiveCallbacks[(int)WebAction.FORCERELEASE] = (obj) => {
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = m_grabbables[webTransform.id] as TLabSyncGrabbable;

                if (grabbable == null) return;

                grabbable.ForceReleaseFromOutside();

                return;
            };
            receiveCallbacks[(int)WebAction.DIVIDEGRABBER] = (obj) => {
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = m_grabbables[webTransform.id] as TLabSyncGrabbable;

                if (grabbable == null) return;

                grabbable.DivideFromOutside(obj.active);

                return;
            };
            receiveCallbacks[(int)WebAction.SYNCTRANSFORM] = (obj) => {
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = m_grabbables[webTransform.id] as TLabSyncGrabbable;

                if (grabbable == null) return;

                grabbable.SyncFromOutside(webTransform);

                return;
            };
            receiveCallbacks[(int)WebAction.SYNCANIM] = (obj) => {
                WebAnimInfo webAnimator = obj.animator;
                TLabSyncAnim syncAnim = m_animators[webAnimator.id] as TLabSyncAnim;

                if (syncAnim == null) return;

                syncAnim.SyncAnimFromOutside(webAnimator);

                return;
            };
            receiveCallbacks[(int)WebAction.CLEARTRANSFORM] = (obj) => { };
            receiveCallbacks[(int)WebAction.CLEARANIM] = (obj) => { };
            receiveCallbacks[(int)WebAction.REFLESH] = (obj) => { };
            receiveCallbacks[(int)WebAction.UNIREFLESHTRANSFORM] = (obj) => { };
            receiveCallbacks[(int)WebAction.UNIREFLESHANIM] = (obj) => { };
            receiveCallbacks[(int)WebAction.CUSTOMACTION] = (obj) => {
                m_customCallbacks[obj.customIndex].OnMessage(obj.custom);

                return;
            };

            websocket = new WebSocket(m_serverAddr);

            websocket.OnOpen += () =>
            {
                string json =
                    "{" +
                        TLabSyncClientConst.ROLE + (m_isHost ? ((int)WebRole.HOST).ToString() : ((int)WebRole.GUEST).ToString()) + TLabSyncClientConst.COMMA +
                        TLabSyncClientConst.ACTION + ((int)WebAction.REGIST).ToString() +
                    "}";

                Debug.Log(thisName + json);

                SendWsMessage(json);

                Debug.Log(thisName + "Connection open!");
            };

            websocket.OnError += (e) =>
            {
                Debug.Log(thisName + "Error! " + e);
            };

            websocket.OnClose += (e) =>
            {
                Debug.Log(thisName + "Connection closed!");
            };

            websocket.OnMessage += (bytes) =>
            {
                string message = System.Text.Encoding.UTF8.GetString(bytes);

                TLabSyncJson obj = JsonUtility.FromJson<TLabSyncJson>(message);

#if UNITY_EDITOR
                Debug.Log(thisName + "OnMessage - " + message);
#endif

                receiveCallbacks[obj.action].Invoke(obj);
            };

            // waiting for messages
            await websocket.Connect();

            yield break;
        }

        private IEnumerator ConnectServerTaskStart()
        {
            yield return null;

            if (websocket != null)
            {
                websocket.Close();
                websocket = null;
            }

            yield return null;

            IAsyncEnumerator<int> task = ConnectServerTask();
            task.MoveNextAsync();

            yield return null;

            task.MoveNextAsync();

            yield break;
        }

        public void ConnectServerAsync()
        {
            StartCoroutine(ConnectServerTaskStart());
        }
        #endregion ConnectServer

        void Start()
        {
            ConnectServerAsync();
        }

        #region RTCMessage
        private unsafe void LongCopy(byte* src, byte* dst, int count)
        {
            // https://github.com/neuecc/MessagePack-CSharp/issues/117

            while (count >= 8)
            {
                *(ulong*)dst = *(ulong*)src;
                dst += 8;
                src += 8;
                count -= 8;
            }
            if (count >= 4)
            {
                *(uint*)dst = *(uint*)src;
                dst += 4;
                src += 4;
                count -= 4;
            }
            if (count >= 2)
            {
                *(ushort*)dst = *(ushort*)src;
                dst += 2;
                src += 2;
                count -= 2;
            }
            if (count >= 1)
            {
                *dst = *src;
            }
        }

        public void OnRTCMessage(string dst, string src, byte[] bytes)
        {
            int offset = bytes[0];
            int nOffset = 1 + offset;
            int dataLen = bytes.Length - offset;

            byte[] nameBytes = new byte[offset];

            unsafe
            {
                // id
                fixed (byte* iniP = nameBytes, iniD = bytes)
                {
                    //for (byte* pt = iniP, pd = iniD + 1; pt < iniP + offset; pt++, pd++) *pt = *pd;
                    LongCopy(iniD + 1, iniP, offset);
                }
            }

            string targetName = System.Text.Encoding.UTF8.GetString(nameBytes);

            TLabSyncGrabbable grabbable = m_grabbables[targetName] as TLabSyncGrabbable;
            if (grabbable == null) return;

            float[] rtcTransform = new float[10];

            unsafe
            {
                // transform
                fixed (byte* iniP = bytes)
                fixed (float* iniD = &(rtcTransform[0]))
                {
                    //for (byte* pt = iniP + nOffset, pd = (byte*)iniD; pt < iniP + nOffset + dataLen; pt++, pd++) *pd = *pt;
                    LongCopy(iniP + nOffset, (byte*)iniD, dataLen);
                }
            }

            WebObjectInfo webTransform = new WebObjectInfo
            {
                position = new WebVector3 { x = rtcTransform[0], y = rtcTransform[1], z = rtcTransform[2] },
                rotation = new WebVector4 { x = rtcTransform[3], y = rtcTransform[4], z = rtcTransform[5], w = rtcTransform[6] },
                scale = new WebVector3 { x = rtcTransform[7], y = rtcTransform[8], z = rtcTransform[9] }
            };

            grabbable.SyncFromOutside(webTransform);
        }

        /// <summary>
        /// WebRTC
        /// </summary>
        /// <param name="bytes"></param>
        public void SendRTCMessage(byte[] bytes)
        {
            dataChannel.SendRTCMsg(bytes);
        }
        #endregion RTCMessage

        #region SendWebsocketMessage
        public void SendWsMessage(
            WebRole role,
            WebAction action,
            int seatIndex = -1,
            bool active = false,
            WebObjectInfo transform = null,
            WebAnimInfo animator = null,
            int customIndex = -1,
            string custom = "")
        {
            TLabSyncJson obj = new TLabSyncJson
            {
                role = (int)role,
                action = (int)action,
                seatIndex = seatIndex,
                active = active,
                transform = transform,
                animator = animator,
                customIndex = customIndex,
                custom = custom
            };

            string json = JsonUtility.ToJson(obj);
            SendWsMessage(json);
        }

        public async void SendWsMessage(string json)
        {
            if (websocket != null && websocket.State == WebSocketState.Open)
                await websocket.SendText(json);
        }
        #endregion SendWebsocketMessage

        public void CloseRTC()
        {
            dataChannel.Exit();
        }

        void Awake()
        {
            Instalce = this;

            if (dataChannel == null)
                dataChannel = GetComponent<TLabWebRTCDataChannel>();
        }

        void Update()
        {
#if !UNITY_WEBGL || UNITY_EDITOR
            if (websocket != null)
                websocket.DispatchMessageQueue();
#endif
        }

        public async void OnDestroy()
        {
            if (websocket != null) await websocket.Close();
            websocket = null;
        }

        private async void OnApplicationQuit()
        {
            if (websocket != null) await websocket.Close();
            websocket = null;
        }
    }
}
