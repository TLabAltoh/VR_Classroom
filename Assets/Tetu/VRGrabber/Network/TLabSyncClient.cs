using System;
using System.Collections;
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
}

public enum WebAnimValueType
{
    typeFloat,
    typeInt,
    typeBool,
    typeTrigger
}

public enum WebRole
{
    server,
    host,
    guest
}

public enum WebAction
{
    regist,
    regect,
    acept,
    guestDisconnect,
    guestParticipation,
    allocateGravity,
    setGravity,
    grabbLock,
    forceRelease,
    syncTransform,
    syncAnim
}

public static class TLabSyncClientConst
{
    // Top
    public const string COMMA       = ",";
    public const string ROLE        = "\"role\":";
    public const string ACTION      = "\"action\":";
    public const string SEATINDEX   = "\"seatIndex\":";
    public const string ACTIVE      = "\"active\":";
    public const string TRANSFORM   = "\"transform\":";
    public const string ANIMATOR    = "\"animator\":";

    // Transform
    public const string TRANSFORM_ID    = "\"id\":";
    public const string RIGIDBODY       = "\"rigidbody\":";
    public const string GRAVITY         = "\"gravity\":";
    public const string POSITION        = "\"position\":";
    public const string ROTATION        = "\"rotation\":";
    public const string SCALE           = "\"scale\":";

    // Animator
    public const string ANIMATOR_ID     = "\"id\":";
    public const string PARAMETER       = "\"parameter\":";
    public const string TYPE            = "\"type\":";

    public const string FLOATVAL        = "\"floatVal\":";
    public const string INTVAL          = "\"intVal\":";
    public const string BOOLVAL         = "\"boolVal\":";
    public const string TRIGGERVAL      = "\"triggerVal\":";

    // WebVector
    public const string X = "\"x\":";
    public const string Y = "\"y\":";
    public const string Z = "\"z\":";
    public const string W = "\"w\":";
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

    private Hashtable m_grabbables = new Hashtable();
    private Hashtable m_animators = new Hashtable();

    private const string prefabName = "OVRControllerPrefab.";

    public Hashtable Grabbables
    {
        get
        {
            return m_grabbables;
        }
    }

    async void Start()
    {
        websocket = new WebSocket(m_serverAddr);

        websocket.OnOpen += () =>
        {
            string json =
                "{" +
                    TLabSyncClientConst.ROLE + ((int)WebRole.guest).ToString() + TLabSyncClientConst.COMMA +
                    TLabSyncClientConst.ACTION + ((int)WebAction.regist).ToString() +
                "}";

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
            string message = System.Text.Encoding.UTF8.GetString(bytes);

            TLabSyncJson obj = JsonUtility.FromJson<TLabSyncJson>(message);

#if UNITY_EDITOR
            Debug.Log("tlabwebsocket: OnMessage - " + message);
#endif

            //
            // Switch by role
            //

            if(obj.role == (int)WebRole.server)
            {
                //
                // from server
                //

                if (obj.action == (int)WebAction.acept)
                {
                    m_seatIndex = obj.seatIndex;

                    if (m_leftHand != null && m_rightHand != null)
                    {
                        m_rightHand.name = prefabName + obj.seatIndex.ToString() + ".RTouch";
                        m_leftHand.name = prefabName + obj.seatIndex.ToString() + ".LTouch";
                        m_rightHand.GetComponent<TLabSyncGrabbable>().m_enableSync = true;
                        m_leftHand.GetComponent<TLabSyncGrabbable>().m_enableSync = true;
                    }

                    //
                    // TLabSyncGrabbable
                    //

                    TLabSyncGrabbable[] grabbables = FindObjectsOfType<TLabSyncGrabbable>();
                    foreach (TLabSyncGrabbable grabbable in grabbables)
                    {
                        m_grabbables[grabbable.gameObject.name] = grabbable;
                    }

                    if (m_registWorldData == true)
                    {
                        foreach (TLabSyncGrabbable grabbable in grabbables)
                        {
                            grabbable.SyncTransform();
                        }
                    }

                    //
                    // Animator
                    //

                    Animator[] animators = FindObjectsOfType<Animator>();
                    foreach (Animator animator in animators)
                    {
                        m_animators[animator.gameObject.name] = animator;
                    }

                    return;
                }
                else if (obj.action == (int)WebAction.guestDisconnect)
                {
                    // now managed by prefab instancing

                    string guestName = prefabName + obj.seatIndex.ToString();

                    GameObject guestRTouch = GameObject.Find(guestName + ".RTouch");
                    GameObject guestLTouch = GameObject.Find(guestName + ".LTouch");
                    GameObject guestHead = GameObject.Find(guestName + ".Head");

                    if(guestRTouch != null)
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

                    Debug.Log("tlabwebsocket: guest disconncted . " + obj.seatIndex.ToString());

                    return;
                }
                else if (obj.action == (int)WebAction.guestParticipation)
                {
                    Vector3 respownPos = new Vector3(0.0f, -0.5f, 0.0f);
                    Quaternion respownRot = Quaternion.identity;

                    string guestName = prefabName + obj.seatIndex.ToString();

                    if (m_guestRTouch != null)
                    {
                        GameObject guestRTouch = Instantiate(m_guestRTouch, respownPos, respownRot);
                        guestRTouch.name = guestName + ".RTouch";

                        m_grabbables[guestRTouch.name] = guestRTouch.GetComponent<TLabSyncGrabbable>();
                    }

                    if(m_guestLTouch != null)
                    {
                        GameObject guestLTouch = Instantiate(m_guestLTouch, respownPos, respownRot);
                        guestLTouch.name = guestName + ".LTouch";

                        m_grabbables[guestLTouch.name] = guestLTouch.GetComponent<TLabSyncGrabbable>();
                    }

                    if(m_guestHead != null)
                    {
                        GameObject guestHead = Instantiate(m_guestHead, respownPos, respownRot);
                        guestHead.name = guestName + ".Head";

                        m_grabbables[guestHead.name] = guestHead.GetComponent<TLabSyncGrabbable>();
                    }

                    Debug.Log("tlabwebsokcet: guest participated . " + obj.seatIndex.ToString());

                    return;
                }
                else if (obj.action == (int)WebAction.allocateGravity)
                {
                    WebObjectInfo webTransform = obj.transform;
                    TLabSyncGrabbable grabbable = m_grabbables[webTransform.id] as TLabSyncGrabbable;
                    if (grabbable != null)
                    {
                        grabbable.AllocateGravity(obj.active);
                    }

                    return;
                }
            }

            //
            // default
            //

            if(obj.action == (int)WebAction.syncAnim)
            {
                //
                // aniamtor is not null
                //

                WebAnimInfo webAnimator = obj.animator;
                Animator animator = m_animators[webAnimator.id] as Animator;

                if (animator == null)
                    return;

                if (webAnimator.type == (int)WebAnimValueType.typeFloat)
                {
                    animator.SetFloat(webAnimator.parameter, webAnimator.floatVal);
                }
                else if (webAnimator.type == (int)WebAnimValueType.typeInt)
                {
                    animator.SetInteger(webAnimator.parameter, webAnimator.intVal);
                }
                else if (webAnimator.type == (int)WebAnimValueType.typeBool)
                {
                    animator.SetBool(webAnimator.parameter, webAnimator.boolVal);
                }
                else if (webAnimator.type == (int)WebAnimValueType.typeTrigger)
                {
                    animator.SetTrigger(webAnimator.parameter);
                }

                return;
            }
            else
            {
                //
                // transform is not null
                //

                WebObjectInfo webTransform = obj.transform;
                TLabSyncGrabbable grabbable = m_grabbables[webTransform.id] as TLabSyncGrabbable;

                if (grabbable == null)
                    return;

                if (obj.action == (int)WebAction.syncTransform)
                {
                    grabbable.SyncRemote(webTransform);
                }
                else if (obj.action == (int)WebAction.setGravity)
                {
                    grabbable.SetGravity(obj.active);
                }
                else if (obj.action == (int)WebAction.grabbLock)
                {
                    grabbable.GrabbLockRemote(obj.active);
                }
                else if (obj.action == (int)WebAction.forceRelease)
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
