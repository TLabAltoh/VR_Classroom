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

    public WebObjectInfo transform;
}

public class TLabSyncClient : MonoBehaviour
{
    [SerializeField] private string m_serverAddr = "ws://192.168.11.10:5000";

    [System.NonSerialized] public static TLabSyncClient Instalce;

    private WebSocket websocket;
    private int m_seatIndex = -1;

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

            Debug.Log("tlabwebsocket: OnMessage - " + message);

            if(obj.role == "server")
            {
                if (obj.action == "acept")
                {
                    m_seatIndex = obj.seatIndex;

                    if (m_seatIndex == 0)
                    {
                        TLabSyncGrabbable[] grabbables = FindObjectsOfType<TLabSyncGrabbable>();
                        foreach (TLabSyncGrabbable grabbable in grabbables)
                            grabbable.SyncTransform();
                    }
                }
                else if (obj.action == "disconnect")
                {
                    Debug.Log("tlabwebsocket: " + "other player disconncted . " + obj.seatIndex.ToString());
                }
            }
            else if(obj.role == "student")
            {
                if (obj.action == "sync transform")
                {
                    WebObjectInfo webTransform = obj.transform;

                    GameObject target = GameObject.Find(webTransform.id);

                    if (target != null)
                    {
                        target.transform.position = new Vector3(webTransform.position.x, webTransform.position.y, webTransform.position.z);
                        target.transform.rotation = new Quaternion(webTransform.rotation.x, webTransform.rotation.y, webTransform.rotation.z, webTransform.rotation.w);
                        target.transform.position = new Vector3(webTransform.position.x, webTransform.position.y, webTransform.position.z);
                    }
                }
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
