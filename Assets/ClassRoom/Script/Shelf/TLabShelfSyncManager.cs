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
    divide,
    loadModel
}

public class TLabShelfSyncManager : TLabShelfManager
{
    private AssetBundle m_assetBundle;

    protected override IEnumerator FadeIn(int objIndex, int anchorIndex)
    {
        // ç¿ê»Ç…ÇæÇÍÇ‡Ç¢Ç»Ç©Ç¡ÇΩÇÁÉXÉLÉbÉv
        if (TLabSyncClient.Instalce.IsGuestExist(anchorIndex) == false && anchorIndex != 0) yield break;

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
        SendWsMessage(json);
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
        SendWsMessage(json);
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
        SendWsMessage(json);
    }

    private void ShareFromOutside(int objIndex)
    {
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
        SendWsMessage(json);
    }

    private void CollectFromOutside(int objIndex)
    {
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
        #region ìríÜ
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

        // íIÇ…í«â¡Ç∑ÇÈ
        // ëºÉvÉåÉCÉÑÅ[Ç…Ç‡íIÇ÷ÇÃí«â¡Çí ím

        #endregion ìríÜ
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

    public void SendWsMessage(string message)
    {
        TLabSyncJson obj = new TLabSyncJson
        {
            role = (int)WebRole.guest,
            action = (int)WebAction.customAction,
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

        return;
    }
}
