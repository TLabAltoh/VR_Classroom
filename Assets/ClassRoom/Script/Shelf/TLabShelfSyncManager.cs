using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using NativeWebSocket;

[System.Serializable]
public class TLabSyncShelfJson
{
    public int action;
    public string url;
    public int objIndex = -1;
}

public enum WebShelfAction
{
    takeOut,
    putAway,
    share,
    collect,
    loadModel
}

public class TLabShelfSyncManager : TLabShelfManager
{
    [SerializeField] private string m_serverAddr;

    private AssetBundle m_assetBundle;
    private WebSocket websocket;

    protected override IEnumerator FadeIn(int objIndex, int anchorIndex)
    {
        base.FadeIn(objIndex, anchorIndex);
        TLabSyncClient.Instalce.ForceReflesh();
        yield break;
    }

    protected override IEnumerator FadeOut(int objIndex, int anchorIndex)
    {
        base.FadeOut(objIndex, anchorIndex);
        TLabSyncClient.Instalce.ForceReflesh();
        yield break;
    }

    #region
    //    TLabSyncGrabbable grabbable = TLabSyncClient.Instalce.Grabbables[shelfObjInfo.obj.gameObject.name] as TLabSyncGrabbable;
    //        if (grabbable != null)
    //        {
    //            grabbable.ForceRelease();
    //            grabbable.GrabbLock(true);
    //            grabbable.GrabbLockSelf(TLabSyncClient.Instalce.SeatIndex);
    //        }

    //GameObject shelfObj = shelfObjInfo.obj;

    //float remain = 2.0f;

    //while (remain > 0.0f)
    //{
    //    remain -= Time.deltaTime;

    //    shelfObj.transform.rotation = Quaternion.AngleAxis(10.0f * Time.deltaTime, Vector3.up) * shelfObj.transform.rotation;

    //    if (grabbable != null)
    //    {
    //        grabbable.SyncTransform();

    //        grabbable.ForceRelease();
    //        grabbable.GrabbLock(true);
    //    }

    //    yield return null;
    //}

    //shelfObj.transform.position = target.transform.position;
    //shelfObj.transform.rotation = target.transform.rotation;
    //shelfObj.transform.localScale = target.transform.localScale;

    //if (grabbable != null)
    //{
    //    grabbable.SyncTransform();

    //    grabbable.GrabbLock(false);
    //    grabbable.GrabbLockSelf(-1);
    //}

    //shelfObjInfo.currentTask = null;
    #endregion

    public override void PutAway(int objIndex)
    {
        base.PutAway(objIndex);

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.putAway,
            objIndex = objIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json);
    }

    private void PutAwayFromOutside(int objIndex)
    {
        StartCoroutine(FadeOut(objIndex, 0));
    }

    public override void TakeOut(int objIndex)
    {
        base.TakeOut(objIndex);

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.takeOut,
            objIndex = objIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json);
    }

    private void TakeOutFromOutside(int objIndex)
    {
        StartCoroutine(FadeIn(objIndex, 0));
    }

    public override void Share(int objIndex)
    {
        base.Share(objIndex);

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.share,
            objIndex = objIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json);
    }

    private void ShareFromOutside(int objIndex)
    {
        for (int i = 1; i < m_anchors.Length; i++)
            StartCoroutine(FadeIn(objIndex, i));
    }

    public override void Collect(int objIndex)
    {
        base.Collect(objIndex);

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.collect,
            objIndex = objIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json);
    }

    private void CollectFromOutside(int objIndex)
    {
        for (int i = 1; i < m_anchors.Length; i++)
            StartCoroutine(FadeOut(objIndex, i));
    }

    public async void SendWsMessage(string json)
    {
        if (websocket.State == WebSocketState.Open)
            await websocket.SendText(json);
    }

    public IEnumerator DownloadAssetBundle(string modURL, int objIndex)
    {
        Debug.Log("Start Load Asset");

        if (m_assetBundle != null)
            m_assetBundle.Unload(false);

        var request = UnityWebRequestAssetBundle.GetAssetBundle(modURL);
        yield return request.SendWebRequest();

        // Handle error
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.LogError(request.error);
            yield break;
        }

        var handler = request.downloadHandler as DownloadHandlerAssetBundle;
        m_assetBundle = handler.assetBundle;

        Debug.Log("Finish Load Asset");

        AssetBundleRequest assetLoadRequest = m_assetBundle.LoadAssetAsync<GameObject>("ROOM");
        yield return assetLoadRequest;

        GameObject prefab = assetLoadRequest.asset as GameObject;

        // ’I‚É’Ç‰Á‚·‚é
        // ‘¼ƒvƒŒƒCƒ„[‚É‚à’I‚Ö‚Ì’Ç‰Á‚ð’Ê’m
    }

    public void LoadModelFromURL(string url, int objIndex)
    {
        StartCoroutine(DownloadAssetBundle(url, objIndex));
    }

    public void LoadModelFromURL(string url)
    {
        StartCoroutine(DownloadAssetBundle(url, 0));

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.loadModel,
            url = url
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json);
    }

    async void Start()
    {
        websocket = new WebSocket(m_serverAddr);

        websocket.OnOpen += () =>
        {
            Debug.Log("tlabsyncshelf: Connection open!");
        };

        websocket.OnError += (e) =>
        {
            Debug.Log("Error! " + e);
        };

        websocket.OnClose += (e) =>
        {
            Debug.Log("tlabsyncshelf: Connection closed!");
        };

        websocket.OnMessage += (bytes) =>
        {
            string message = System.Text.Encoding.UTF8.GetString(bytes);
            TLabSyncShelfJson obj = JsonUtility.FromJson<TLabSyncShelfJson>(message);

#if UNITY_EDITOR
            Debug.Log("tlabsyncshelf: OnMessage - " + message);
#endif

            if (obj.action == (int)WebShelfAction.loadModel)
                LoadModelFromURL(obj.url, obj.objIndex);
            else if (obj.action == (int)WebShelfAction.takeOut)
                TakeOutFromOutside(obj.objIndex);
            else if (obj.action == (int)WebShelfAction.putAway)
                PutAwayFromOutside(obj.objIndex);
            else if (obj.action == (int)WebShelfAction.share)
                ShareFromOutside(obj.objIndex);
            else if (obj.action == (int)WebShelfAction.collect)
                CollectFromOutside(obj.objIndex);
        };

        await websocket.Connect();
    }
}
