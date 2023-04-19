using UnityEngine;
using NativeWebSocket;

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

    public int seatIndex;

    public string id;

    public float positionX;
    public float positionY;
    public float positionZ;

    public float rotationX;
    public float rotationY;
    public float rotationZ;

    public float scaleX;
    public float scaleY;
    public float scaleZ;
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

            if(obj.role == "server" && obj.action == "acept")
            {
                m_seatIndex = obj.seatIndex;

                if(m_seatIndex == 0)
                {
                    TLabSyncGrabbable[] grabbables = FindObjectsOfType<TLabSyncGrabbable>();
                    foreach(TLabSyncGrabbable grabbable in grabbables)
                        grabbable.SyncTransform();
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
