using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using NativeWebSocket;
using TLab.XR.Interact;
using TLab.XR.Humanoid;
using TLab.Network.WebRTC;

namespace TLab.XR.Network
{
    [RequireComponent(typeof(WebRTCDataChannel))]
    public class SyncClient : MonoBehaviour
    {
        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        [Header("Sync Server Address (port 5000)")]
        [SerializeField] private string m_serverAddr = "ws://192.168.11.10:5000";
        [SerializeField] private string m_roomName = "VR_Classroom";

        [Header("Body Tracker")]
        [SerializeField] private Transform m_cameraRig;
        [SerializeField] private BodyTracker.TrackTarget[] m_trackTargets;

        [Tooltip("The avatar model of the other party as seen from you")]
        [Header("Guest Avator Preset")]
        [SerializeField] private AvatorConfig m_avatorConfig;

        [Tooltip("Responce position of each player")]
        [Header("Respown Anchor")]
        [SerializeField] private Transform[] m_respownAnchors;
        [SerializeField] private Transform m_instantiateAnchor;

        [Tooltip("WebRTCDatachannel for synchronizing Transforms between players")]
        [Header("WebRTCDataChannel")]
        [SerializeField] private WebRTCDataChannel m_dataChannel;

        [Tooltip("Custom message callbacks")]
        [Header("Custom Event")]
        [SerializeField] private CustomCallback[] m_customCallbacks;

        [Tooltip("Whether the user is Host")]
        [Header("User Role")]
        [SerializeField] private bool m_isHost = false;

        [SerializeField] private bool m_buildDebug = false;

#if UNITY_EDITOR
        [SerializeField] private bool m_editorDebug = false;
#endif

        public static SyncClient Instance;

        public static bool Initialized => Instance != null;

        private WebSocket m_websocket;

        public const int SEAT_LENGTH = 5;
        public const int HOST_INDEX = 0;
        public const int NOT_REGISTED = -1;

        private int m_seatIndex = NOT_REGISTED;
        private bool[] m_guestTable = new bool[SEAT_LENGTH];

        // 大文字にしたい
        private const string COMMA = ".";
        private const string PREFAB_NAME = "OVR_GUEST_ANCHOR";
        private Queue<GameObject>[] m_avatorInstanceQueue = new Queue<GameObject>[SEAT_LENGTH];

        private delegate void ReceiveCallback(TLabSyncJson obj);

        ReceiveCallback[] receiveCallbacks = new ReceiveCallback[(int)WebAction.CUSTOMACTION + 1];

        public int seatIndex => m_seatIndex;

        public int seatLength => SEAT_LENGTH;

        public bool socketIsNull => m_websocket == null;

        public bool registed => socketIsNull ? false : m_seatIndex != NOT_REGISTED;

        public bool socketIsOpen => socketIsNull ? false : m_websocket.State == WebSocketState.Open;

        public bool socketIsConnecting => socketIsNull ? false : m_websocket.State == WebSocketState.Connecting;

        public bool isHost { get => m_isHost; set => m_isHost = value; }

        public string serverAddr => m_serverAddr;

        public void SetServerAddr(string addr) => m_serverAddr = addr;

        public bool IsGuestExist(int index) => m_guestTable[index];

        public void CacheAvator(int seatIndex, GameObject go)
        {
            if (m_avatorInstanceQueue[seatIndex] == null)
            {
                m_avatorInstanceQueue[seatIndex] = new Queue<GameObject>();
            }

            m_avatorInstanceQueue[seatIndex].Enqueue(go);
        }

        public void DeleteAvator(int seatIndex)
        {
            var avatorInstanceQueue = m_avatorInstanceQueue[seatIndex];
            if (avatorInstanceQueue != null)
            {
                while (avatorInstanceQueue.Count > 0)
                {
                    var go = avatorInstanceQueue.Dequeue();
                    BodyTracker.ClearObject(go);
                }
            }
        }

        #region REFLESH
        /// <summary>
        /// Let the server organize cached object information (e.g., Rigidbody allocation)
        /// Request the results of organized object information.
        /// </summary>
        /// <param name="reloadWorldData"></param>
        public void ForceReflesh(bool reloadWorldData)
        {
            var obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.REFLESH,
                active = reloadWorldData
            };
            SendWsMessage(JsonUtility.ToJson(obj));

            Debug.Log(THIS_NAME + "force reflesh");
        }

        /// <summary>
        /// Refresh only specific objects.
        /// </summary>
        /// <param name="targetName"></param>
        public void UniReflesh(string targetName)
        {
            var obj = new TLabSyncJson
            {
                role = (int)WebRole.GUEST,
                action = (int)WebAction.UNIREFLESHTRANSFORM,
                transform = new WebObjectInfo { id = targetName }
            };
            SendWsMessage(JsonUtility.ToJson(obj));

            Debug.Log(THIS_NAME + "reflesh " + targetName);
        }
        #endregion REFLESH

        #region CONNECT_SERVER
        private string GetBodyTrackerName(string playerName, int seatIndex, AvatorConfig.BodyParts parts)
        {
            // playerNameは現在使用していない

            return PREFAB_NAME + COMMA + seatIndex.ToString() + COMMA + parts.ToString();
        }

        /// <summary>
        /// Send exit notice to the server.
        /// Exit only and do not close the socket
        /// </summary>
        public void Exit()
        {
            string json =
                "{" +
                    SyncClientConst.ROLE + (m_isHost ? ((int)WebRole.HOST).ToString() : ((int)WebRole.GUEST).ToString()) + SyncClientConst.COMMA +
                    SyncClientConst.ACTION + ((int)WebAction.EXIT).ToString() + SyncClientConst.COMMA +
                    SyncClientConst.SEATINDEX + (m_seatIndex.ToString()) +
                "}";

            SendWsMessage(json);
        }

        /// <summary>
        /// Coroutine to connect to Websocket server asynchronously.
        /// Control execution timing from ConnectServerTaskStart().
        /// I want to use await, so I used IAsyncEnumrator.
        /// </summary>
        /// <returns></returns>
        private async IAsyncEnumerator<int> ConnectServerTask()
        {
            yield return -1;

            receiveCallbacks[(int)WebAction.REGIST] = (obj) => { };
            receiveCallbacks[(int)WebAction.REGECT] = (obj) => { };
            receiveCallbacks[(int)WebAction.ACEPT] = (obj) => {

                // Permission to join is granted by the server

                m_seatIndex = obj.seatIndex;

                m_guestTable[obj.seatIndex] = true;

                // Enable sync own avator

                foreach (var trackTarget in m_trackTargets)
                {
                    var parts = trackTarget.parts;
                    var target = trackTarget.target;

                    var trackerName = GetBodyTrackerName("", obj.seatIndex, parts);
                    target.name = trackerName;

                    var tracker = target.gameObject.AddComponent<BodyTracker>();
                    tracker.enableSync = true;
                    tracker.self = true;

                    CacheAvator(obj.seatIndex, tracker.gameObject);
                }

                if (m_instantiateAnchor != null)
                {
                    m_cameraRig.SetLocalPositionAndRotation(m_instantiateAnchor.position, m_instantiateAnchor.rotation);
                }
                else
                {
                    m_cameraRig.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                }

                var anchor = m_respownAnchors[m_seatIndex];
                m_cameraRig.SetPositionAndRotation(anchor.position, anchor.rotation);

                // Connect to signaling server
                var userID = gameObject.name + "_" + m_seatIndex.ToString();
                var roomID = m_roomName;
                m_dataChannel.Join(userID, roomID);

            };
            receiveCallbacks[(int)WebAction.EXIT] = (obj) => { };
            receiveCallbacks[(int)WebAction.GUESTDISCONNECT] = (obj) => {

                // Guest disconnected

                // If guest is not exist
                if (!m_guestTable[obj.seatIndex])
                {
                    return;
                }

                DeleteAvator(obj.seatIndex);

                m_guestTable[obj.seatIndex] = false;

                foreach (var callback in m_customCallbacks)
                {
                    callback.OnGuestDisconnected(obj.seatIndex);
                }

                Debug.Log(THIS_NAME + "guest disconncted: " + obj.seatIndex.ToString());

            };
            receiveCallbacks[(int)WebAction.GUESTPARTICIPATION] = (obj) => {

                // Processing when guest joins

                // If guest already exists
                if (m_guestTable[obj.seatIndex])
                {
                    return;
                }

                // Visualize avatars of newly joined players

                foreach (var avatorParts in m_avatorConfig.body)
                {
                    var parts = avatorParts.parts;
                    var prefab = avatorParts.prefab;

                    var guestParts = null as GameObject;

                    if (m_instantiateAnchor != null)
                    {
                        guestParts = Instantiate(prefab, m_instantiateAnchor.position, m_instantiateAnchor.rotation);
                    }
                    else
                    {
                        guestParts = Instantiate(prefab);
                    }

                    var trackerName = GetBodyTrackerName("", obj.seatIndex, parts);
                    guestParts.name = trackerName;

                    // TODO: Instantiate()の後，Start()が呼び出されるタイミングを調査する
                    //var networkedObj = guestParts.GetComponent<NetworkedObject>();

                    //if (networkedObj != null)
                    //{
                    //    NetworkedObject.Register(guestParts.name, networkedObj);
                    //}

                    CacheAvator(obj.seatIndex, guestParts);
                }

                m_guestTable[obj.seatIndex] = true;

                foreach (var callback in m_customCallbacks)
                {
                    callback.OnGuestParticipated(obj.seatIndex);
                }

                Debug.Log(THIS_NAME + "guest participated: " + obj.seatIndex.ToString());

                return;
            };
            receiveCallbacks[(int)WebAction.ALLOCATEGRAVITY] = (obj) => {

                // Object's gravity allocation

                var rigidbodyAllocated = obj.active;
                var webTransform = obj.transform;
                var controller = ExclusiveController.GetById(webTransform.id);
                controller?.AllocateGravity(rigidbodyAllocated);

            };
            receiveCallbacks[(int)WebAction.REGISTRBOBJ] = (obj) => { };
            receiveCallbacks[(int)WebAction.GRABBLOCK] = (obj) => {

                // Grabb lock from outside

                var seatIndex = obj.seatIndex;
                var webTransform = obj.transform;
                var controller = ExclusiveController.GetById(webTransform.id);
                controller?.GrabbLock(seatIndex);

            };
            receiveCallbacks[(int)WebAction.FORCERELEASE] = (obj) => {

                // Force release request

                var self = false;
                var webTransform = obj.transform;
                var controller = ExclusiveController.GetById(webTransform.id);
                controller?.ForceRelease(self);

            };
            receiveCallbacks[(int)WebAction.DIVIDEGRABBER] = (obj) => {

                // Divide grabbable object

                var webTransform = obj.transform;
                var controller = ExclusiveController.GetById(webTransform.id);
                controller?.Divide(obj.active);

            };
            receiveCallbacks[(int)WebAction.SYNCTRANSFORM] = (obj) => {

                // Sync transform

                var webTransform = obj.transform;
                var networkedObject = NetworkedObject.GetById(webTransform.id);
                networkedObject?.SyncFromOutside(webTransform);

            };
            receiveCallbacks[(int)WebAction.SYNCANIM] = (obj) => {

                // Sync animation

                var webAnimator = obj.animator;
                var syncAnim = SyncAnimator.GetById(webAnimator.id);
                syncAnim?.SyncAnimFromOutside(webAnimator);

            };
            receiveCallbacks[(int)WebAction.CLEARTRANSFORM] = (obj) => { };
            receiveCallbacks[(int)WebAction.CLEARANIM] = (obj) => { };
            receiveCallbacks[(int)WebAction.REFLESH] = (obj) => { };
            receiveCallbacks[(int)WebAction.UNIREFLESHTRANSFORM] = (obj) => { };
            receiveCallbacks[(int)WebAction.UNIREFLESHANIM] = (obj) => { };
            receiveCallbacks[(int)WebAction.CUSTOMACTION] = (obj) => {

                m_customCallbacks[obj.customIndex].OnMessage(obj.custom);

            };

            m_websocket = new WebSocket(m_serverAddr);

            m_websocket.OnOpen += () =>
            {
                string json =
                    "{" +
                        SyncClientConst.ROLE + (m_isHost ? ((int)WebRole.HOST).ToString() : ((int)WebRole.GUEST).ToString()) + SyncClientConst.COMMA +
                        SyncClientConst.ACTION + ((int)WebAction.REGIST).ToString() +
                    "}";

                Debug.Log(THIS_NAME + json);

                SendWsMessage(json);

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

                TLabSyncJson obj = JsonUtility.FromJson<TLabSyncJson>(message);

#if UNITY_EDITOR
                Debug.Log(THIS_NAME + "OnMessage - " + message);
#endif

                receiveCallbacks[obj.action].Invoke(obj);
            };

            // waiting for messages
            await m_websocket.Connect();

            yield break;
        }

        /// <summary>
        /// Process to control the execution timing of a coroutine that 
        /// connects to the server asynchronously.
        /// When reconnecting, close the socket once and reconnect.
        /// </summary>
        /// <returns></returns>
        private IEnumerator ConnectServerTaskStart()
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

        /// <summary>
        /// Connect to a Websocekt server asynchronously
        /// </summary>
        public void ConnectServerAsync()
        {
            StartCoroutine(ConnectServerTaskStart());
        }
        #endregion CONNECT_SERVER

        #region RTC_MESSAGE

        private static unsafe void LongCopy(byte* src, byte* dst, int count)
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

            var networkedObject = NetworkedObject.GetById(targetName);
            if (networkedObject == null)
            {
                Debug.Log("not found");

                return;
            }

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

            var webTransform = new WebObjectInfo
            {
                position = new WebVector3 { x = rtcTransform[0], y = rtcTransform[1], z = rtcTransform[2] },
                rotation = new WebVector4 { x = rtcTransform[3], y = rtcTransform[4], z = rtcTransform[5], w = rtcTransform[6] },
                scale = new WebVector3 { x = rtcTransform[7], y = rtcTransform[8], z = rtcTransform[9] }
            };

            networkedObject.SyncFromOutside(webTransform);
        }

        public void SendRTCMessage(byte[] bytes)
        {
            m_dataChannel.SendRTCMsg(bytes);
        }
        #endregion RTC_MESSAGE

        #region WEBSOCKET_MESSAGE
        public void SendWsMessage(WebRole role, WebAction action,
                                  int seatIndex = -1, bool active = false,
                                  WebObjectInfo transform = null, WebAnimInfo animator = null, int customIndex = -1, string custom = "")
        {
            var obj = new TLabSyncJson
            {
                role = (int)role, action = (int)action,
                seatIndex = seatIndex, active = active,
                transform = transform, animator = animator, customIndex = customIndex, custom = custom
            };
            SendWsMessage(JsonUtility.ToJson(obj));
        }

        public async void SendWsMessage(string json)
        {
            if (socketIsOpen)
            {
                await m_websocket.SendText(json);
            }
        }
        #endregion WEBSOCKET_MESSAGE

        public void CloseRTC()
        {
            m_dataChannel.Exit();
        }

        public void ConfirmRTCCallbackRegisted()
        {
            if (m_dataChannel == null)
            {
                m_dataChannel = GetComponent<WebRTCDataChannel>();
            }

            if (m_dataChannel.eventCount == 0)
            {
                m_dataChannel.SetCallback(OnRTCMessage);
            }
        }

        void Reset()
        {
            ConfirmRTCCallbackRegisted();
        }

        void Awake()
        {
            Instance = this;

            if (m_dataChannel == null)
            {
                m_dataChannel = GetComponent<WebRTCDataChannel>();
            }
        }

        void Start()
        {
            for (int i = 0; i < m_avatorInstanceQueue.Length; i++)
            {
                if (m_avatorInstanceQueue[i] != null)
                {
                    m_avatorInstanceQueue[i] = new Queue<GameObject>();
                }
            }

            ConnectServerAsync();

#if UNITY_EDITOR
            if (m_editorDebug)
            {
                m_isHost = true;
            }
#endif

            if (m_buildDebug)
            {
#if UNITY_EDITOR
                m_isHost = false;
#elif UNITY_ANDROID
                m_isHost = true;
#endif
            }
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
            CloseWebSocket();
        }

        void OnApplicationQuit()
        {
            CloseWebSocket();
        }
    }
}
