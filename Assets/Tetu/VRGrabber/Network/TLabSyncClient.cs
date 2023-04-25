using UnityEngine;
using NativeWebSocket;

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
public class TLabSyncJson
{
    // .js code
    //var obj = {
    //    role: "host",
    //    action: "update obj",
    //    id: id,
    //    position: position,
    //    scale: scale,
    //    rotation: rotation
    //};

    public string role;
    public string action;

    public int seatIndex = -1;

    public bool active = false;

    public WebObjectInfo transform;
}

public class TLabSyncClient : MonoBehaviour
{
    [SerializeField] private string m_serverAddr = "ws://192.168.11.10:5000";
    [SerializeField] private bool m_registWorldData = false;
    [SerializeField] private GameObject m_rightHand;
    [SerializeField] private GameObject m_leftHand;

    [Header("Guest Avator")]
    [SerializeField] private GameObject m_guestRTouch;
    [SerializeField] private GameObject m_guestLTouch;
    [SerializeField] private GameObject m_guestHead;

    [System.NonSerialized] public static TLabSyncClient Instalce;

    private WebSocket websocket;
    private int m_seatIndex = -1;

    private TLabSyncGrabbable GetTargetGrabbable(string id)
    {
        GameObject target = GameObject.Find(id);

        if (target != null)
        {
            return target.GetComponent<TLabSyncGrabbable>();
        }
        else
        {
            return null;
        }
    }

    async void Start()
    {
        websocket = new WebSocket(m_serverAddr);

        websocket.OnOpen += () =>
        {
            // .js code
            //console.log("socket connected");
            //var obj = {
            //    role: "host",
            //    action: "regist"
            //};
            //var json = JSON.stringify(obj);
            //sock.send(json);

            TLabSyncJson obj = new TLabSyncJson
            {
                role = "student",
                action = "regist"
            };

            string json = JsonUtility.ToJson(obj);

            Debug.Log("tlabwebsocket: " + json);

            SendWsMessage(json);

            Debug.Log("tlabwebsocket: Connection open!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("tlabwebsocket: Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            // .js code
            //if (obj.action === "update obj")
            //{
            //    var target = document.getElementById(obj.id);
            //    target.setAttribute("position", obj.position);
            //    target.setAttribute("scale", obj.scale);
            //    target.setAttribute("rotation", obj.rotation);
            //}

            string message = System.Text.Encoding.UTF8.GetString(bytes);

            TLabSyncJson obj = JsonUtility.FromJson<TLabSyncJson>(message);

#if UNITY_EDITOR
            Debug.Log("tlabwebsocket: OnMessage - " + message);
#endif

            //
            // Switch by role
            //

            if(obj.role == "server")
            {
                if (obj.action == "acept")
                {
                    m_seatIndex = obj.seatIndex;

                    if (m_leftHand != null && m_rightHand != null)
                    {
                        m_rightHand.name = "OVRControllerPrefab." + obj.seatIndex.ToString() + ".RTouch";
                        m_leftHand.name = "OVRControllerPrefab." + obj.seatIndex.ToString() + ".LTouch";
                        m_rightHand.GetComponent<TLabSyncGrabbable>().m_enableSync = true;
                        m_leftHand.GetComponent<TLabSyncGrabbable>().m_enableSync = true;
                    }

                    if (m_registWorldData == true)
                    {
                        TLabSyncGrabbable[] grabbables = FindObjectsOfType<TLabSyncGrabbable>();
                        foreach (TLabSyncGrabbable grabbable in grabbables)
                        {
                            grabbable.SyncTransform();
                        }
                    }

                    return;
                }
                else if (obj.action == "guest disconnect")
                {
                    // now managed by prefab instancing

                    GameObject guestRTouch = GameObject.Find("OVRControllerPrefab." + obj.seatIndex.ToString() + ".RTouch");
                    GameObject guestLTouch = GameObject.Find("OVRControllerPrefab." + obj.seatIndex.ToString() + ".LTouch");
                    GameObject guestHead = GameObject.Find("OVRControllerPrefab." + obj.seatIndex.ToString() + ".Head");

                    if(guestRTouch != null)
                    {
                        Object.Destroy(guestRTouch);
                    }

                    if (guestLTouch != null)
                    {
                        Object.Destroy(guestLTouch);
                    }

                    if (guestHead != null)
                    {
                        Object.Destroy(guestHead);
                    }

                    Debug.Log("tlabwebsocket: guest disconncted . " + obj.seatIndex.ToString());

                    return;
                }
                else if (obj.action == "guest participation")
                {
                    if(m_guestRTouch != null)
                    {
                        GameObject guestRTouch = Instantiate(m_guestRTouch, new Vector3(0.0f, -0.5f, 0.0f), Quaternion.identity);
                        guestRTouch.name = "OVRControllerPrefab." + obj.seatIndex.ToString() + ".RTouch";
                    }

                    if(m_guestLTouch != null)
                    {
                        GameObject guestLTouch = Instantiate(m_guestLTouch, new Vector3(0.0f, -0.5f, 0.0f), Quaternion.identity);
                        guestLTouch.name = "OVRControllerPrefab." + obj.seatIndex.ToString() + ".LTouch";
                    }

                    if(m_guestHead != null)
                    {
                        GameObject guestHead = Instantiate(m_guestHead, new Vector3(0.0f, -0.5f, 0.0f), Quaternion.identity);
                        guestHead.name = "OVRControllerPrefab." + obj.seatIndex.ToString() + ".Head";
                    }

                    Debug.Log("tlabwebsokcet: guest participated . " + obj.seatIndex.ToString());

                    return;
                }
                else if (obj.action == "allocate gravity")
                {
                    WebObjectInfo webTransform = obj.transform;
                    TLabSyncGrabbable grabbable = GetTargetGrabbable(webTransform.id);
                    if (grabbable != null)
                    {
                        grabbable.AllocateGravity(obj.active);
                    }

                    return;
                }
            }

            //
            // Default
            //

            if (obj.action == "sync transform")
            {
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = GetTargetGrabbable(webTransform.id);
                if (grabbable != null)
                {
                    grabbable.SyncRemote(webTransform);
                }
            }
            else if (obj.action == "set gravity")
            {
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = GetTargetGrabbable(webTransform.id);
                if (grabbable != null)
                {
                    grabbable.SetGravity(obj.active);
                }

                return;
            }
            else if (obj.action == "grabb lock")
            {
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = GetTargetGrabbable(webTransform.id);
                if (grabbable != null)
                {
                    grabbable.GrabbLockRemote(obj.active);
                }
            }
            else if (obj.action == "force release")
            {
                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = GetTargetGrabbable(webTransform.id);
                if (grabbable != null)
                {
                    grabbable.ForceReleaseRemote();
                }

                return;
            }
        };

        // Keep sending messages at every 0.3s
        // InvokeRepeating("SendWebSocketMessage", 0.0f, 0.3f);

        // waiting for messages
        await websocket.Connect();
    }

    public async void SendWsMessage(string json)
    {
        if (websocket.State == WebSocketState.Open)
        {
            await websocket.SendText(json);
        }
    }

    void Awake()
    {
        Instalce = this;
    }

    void Update()
    {
#if !UNITY_WEBGL || UNITY_EDITOR
        websocket.DispatchMessageQueue();
#endif
    }

    private async void OnApplicationQuit()
    {
        await websocket.Close();
    }

}
