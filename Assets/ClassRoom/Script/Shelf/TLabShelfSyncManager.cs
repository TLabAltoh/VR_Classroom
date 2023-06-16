using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

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
    [SerializeField] public TLabInputField m_inputField;
    private AssetBundle m_assetBundle;
    private int m_lastShared = -1;

#if UNITY_EDITOR
    [SerializeField] private string m_testURL;

    public void SetServerAddr(string url)
    {
        m_testURL = url;
    }
#endif

    protected override IEnumerator FadeIn(int objIndex, int anchorIndex)
    {
        // 座席にだれもいなかったらスキップ
        if (TLabSyncClient.Instalce.IsGuestExist(anchorIndex) == false) yield break;

        yield return base.FadeIn(objIndex, anchorIndex);
        TLabSyncClient.Instalce.ForceReflesh();
        yield break;
    }

    protected override IEnumerator FadeOut(int objIndex, int anchorIndex)
    {
        yield return base.FadeOut(objIndex, anchorIndex);
        TLabSyncClient.Instalce.ForceReflesh();
        yield break;
    }

    public override void PutAway()
    {
        base.PutAway();

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.putAway,
            objIndex = m_currentObjIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    private void PutAwayFromOutside(int objIndex)
    {
        StartCoroutine(FadeOut(objIndex, 0));
    }

    public override void TakeOut()
    {
        base.TakeOut();

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.takeOut,
            objIndex = m_currentObjIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    private void TakeOutFromOutside(int objIndex)
    {
        StartCoroutine(FadeIn(objIndex, 0));
    }

    public override void Share()
    {
        base.Share();

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.share,
            objIndex = m_currentObjIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    private void ShareFromOutside(int objIndex)
    {
        m_lastShared = objIndex;

        for (int i = 1; i < m_anchors.Length; i++)
            StartCoroutine(FadeIn(objIndex, i));
    }

    public override void Collect()
    {
        base.Collect();

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.collect,
            objIndex = m_currentObjIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    private void CollectFromOutside(int objIndex)
    {
        m_lastShared = -1;

        for (int i = 1; i < m_anchors.Length; i++)
            StartCoroutine(FadeOut(objIndex, i));
    }

    public void Divide(int objIndex)
    {
        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];
        GameObject go = null;
        shelfObjInfo.instanced.TryGetValue(0, out go);

        if (go == null)
            return;
        else
        {
            TLabSyncGrabbable grabbable = go.GetComponent<TLabSyncGrabbable>();
            grabbable.Devide();
        }
    }

    public IEnumerator DownloadAssetBundle(string modURL, int objIndex)
    {
#if UNITY_EDITOR
        Debug.Log("Start Load Asset");
#endif

        if (m_assetBundle != null)
            m_assetBundle.Unload(false);

        var request = UnityWebRequestAssetBundle.GetAssetBundle(modURL);
        yield return request.SendWebRequest();

        // Handle error
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError ||
            request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.LogError(request.error);
            yield break;
        }

        var handler = request.downloadHandler as DownloadHandlerAssetBundle;
        m_assetBundle = handler.assetBundle;

#if UNITY_EDITOR
        Debug.Log("Finish Load Asset");
#endif

        AssetBundleRequest assetLoadRequest = m_assetBundle.LoadAllAssetsAsync<GameObject>();
        yield return assetLoadRequest;

        GameObject prefab = assetLoadRequest.allAssets[0] as GameObject;

        m_shelfObjInfos[objIndex].obj = prefab;
    }

    public void LoadModelFromURL(string url, int objIndex)
    {
        StartCoroutine(DownloadAssetBundle(url, objIndex));
    }

    public void LoadModelFromURL()
    {
        LoadModelFromURL(m_inputField.text, 2);

        // 他プレイヤーにも棚への追加を通知
        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.loadModel,
            url = m_inputField.text,
            objIndex = 2
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    public void SendWsMessage(string message, int anchorIndex)
    {
        TLabSyncJson obj = new TLabSyncJson
        {
            role = (int)WebRole.guest,
            action = (int)WebAction.customAction,
            seatIndex = anchorIndex,
            custom = message
        };
        string json = JsonUtility.ToJson(obj);

        TLabSyncClient.Instalce.SendWsMessage(json);

        return;
    }

    public void OnMessage(string message)
    {
        TLabSyncShelfJson obj = JsonUtility.FromJson<TLabSyncShelfJson>(message);

#if UNITY_EDITOR
        Debug.Log("tlabsyncshelf: OnMessage - " + message);
#endif

        if (obj.action == (int)WebShelfAction.loadModel)    LoadModelFromURL(obj.url, obj.objIndex);
        else if (obj.action == (int)WebShelfAction.takeOut) TakeOutFromOutside(obj.objIndex);
        else if (obj.action == (int)WebShelfAction.putAway) PutAwayFromOutside(obj.objIndex);
        else if (obj.action == (int)WebShelfAction.share)   ShareFromOutside(obj.objIndex);
        else if (obj.action == (int)WebShelfAction.collect) CollectFromOutside(obj.objIndex);

        return;
    }

    public void OnGuestParticipated(int anchorIndex)
    {
        //
        // 自分の座席にオブジェクトがあることを通知する．
        //

        if (m_lastShared == -1 || m_shelfObjInfos[TLabSyncClient.Instalce.SeatIndex].obj == null) return;

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.share,
            objIndex = m_lastShared
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, anchorIndex);
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadModelFromURL(m_testURL, 2);

            TLabSyncShelfJson obj = new TLabSyncShelfJson
            {
                action = (int)WebShelfAction.loadModel,
                url = m_testURL,
                objIndex = 2
            };
            string json = JsonUtility.ToJson(obj);
            SendWsMessage(json, -1);
        }
#endif
    }
}
