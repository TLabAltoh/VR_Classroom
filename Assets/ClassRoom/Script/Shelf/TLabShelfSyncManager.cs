using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
#if UNITY_EDITOR
using UnityEditor;
#endif

[System.Serializable]
public class TLabSyncShelfJson
{
    public int action;
    public string url;
    public int objIndex = -1;
}

public enum WebShelfAction
{
    TAKEOUT,
    PUTAWAY,
    SHARE,
    COLLECT,
    LOADMODEL
}

public class TLabShelfSyncManager : TLabShelfManager
{
    [SerializeField] public TLabInputField m_inputField;
    private string m_lastLoadURL = "";
    private AssetBundle m_assetBundle;
    private List<int> m_currentShareds = new List<int>();
    private List<int> m_currentTakeOuts = new List<int>();

    private const string thisName = "[tlabsyncshelf] ";

    [SerializeField] private string m_testURL;

    public void SetServerAddr(string url)
    {
        m_testURL = url;
    }

    protected override IEnumerator FadeIn(int objIndex, int anchorIndex)
    {
        // 座席にだれもいなかったら終了
        if (TLabSyncClient.Instalce.IsGuestExist(anchorIndex) == false) yield break;

        // オブジェクトのフェードイン
        yield return base.FadeIn(objIndex, anchorIndex);

        // フェードインしたオブジェクトは自分の席ではない
        // -------> 現在のサーバのTransformと同期
        bool reloadWorldData = TLabSyncClient.Instalce.SeatIndex != anchorIndex;

        if (reloadWorldData)
        {
            string objName = m_shelfObjInfos[objIndex].instanced[anchorIndex].name;
            TLabSyncClient.Instalce.UniReflesh(objName);
        }

        Debug.Log(thisName + "fade in");

        yield break;
    }

    protected override IEnumerator FadeOut(int objIndex, int anchorIndex)
    {
        // 配列の範囲外だったらスキップ
        if (objIndex >= m_shelfObjInfos.Length) yield break;

        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

        // 配列に値が存在しなかったらスキップ
        if (shelfObjInfo == null) yield break;

        GameObject instanced;
        shelfObjInfo.instanced.TryGetValue(anchorIndex, out instanced);

        // インスタンスが存在しなかったらスキップ
        if (instanced == null) yield break;

        // サーバーのキャッシュを削除
        foreach(TLabSyncGrabbable grabbable in instanced.GetComponentsInChildren<TLabSyncGrabbable>())
        {
            grabbable.ShutdownGrabber(true);
            yield return null;
        }

        // インスタンスの削除
        shelfObjInfo.instanced.Remove(anchorIndex);
        Destroy(instanced);

        Debug.Log(thisName + "fade out");

        yield break;
    }

    public override void TakeOut()
    {
        base.TakeOut();

        m_currentTakeOuts.Add(m_currentObjIndex);

        SendShelfActionMessage(action: WebShelfAction.TAKEOUT, objIndex: m_currentObjIndex);
    }

    public override void PutAway()
    {
        base.PutAway();

        m_currentTakeOuts.Remove(m_currentObjIndex);

        SendShelfActionMessage(action: WebShelfAction.PUTAWAY, objIndex: m_currentObjIndex);
    }

    public override void Share()
    {
        base.Share();

        m_currentShareds.Add(m_currentObjIndex);

        SendShelfActionMessage(action: WebShelfAction.SHARE, objIndex: m_currentObjIndex);
    }

    public override void Collect()
    {
        base.Collect();

        m_currentShareds.Remove(m_currentObjIndex);

        SendShelfActionMessage(action: WebShelfAction.COLLECT, objIndex: m_currentObjIndex);
    }

    #region FromOutside

    private void TakeOutFromOutside(int objIndex)
    {
        StartCoroutine(FadeIn(objIndex, 0));
    }

    private void PutAwayFromOutside(int objIndex)
    {
        StartCoroutine(FadeOut(objIndex, 0));
    }

    private void ShareFromOutside(int objIndex)
    {
        m_currentShareds.Add(objIndex);

        for (int i = 1; i < m_anchors.Length; i++) StartCoroutine(FadeIn(objIndex, i));
    }

    private void CollectFromOutside(int objIndex)
    {
        m_currentShareds.Remove(objIndex);

        for (int i = 1; i < m_anchors.Length; i++) StartCoroutine(FadeOut(objIndex, i));
    }

    #endregion FromOutside

    #region LoadModelFromURL

    public IEnumerator DownloadAssetBundle(string modURL, int objIndex)
    {
#if UNITY_EDITOR
        Debug.Log(thisName + "Start Load Asset");
#endif

        if (m_assetBundle != null) m_assetBundle.Unload(false);

        var request = UnityWebRequestAssetBundle.GetAssetBundle(modURL);
        yield return request.SendWebRequest();

        // Handle error
        if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.LogError(thisName + request.error);
            yield break;
        }

        var handler = request.downloadHandler as DownloadHandlerAssetBundle;
        m_assetBundle = handler.assetBundle;

#if UNITY_EDITOR
        Debug.Log(thisName + "Finish Load Asset");
#endif

        AssetBundleRequest assetLoadRequest = m_assetBundle.LoadAllAssetsAsync<GameObject>();
        yield return assetLoadRequest;

        GameObject prefab = assetLoadRequest.allAssets[0] as GameObject;

        m_shelfObjInfos[objIndex].obj = prefab;
    }

    public void LoadModelFromURL(string url, int objIndex)
    {
        if (m_lastLoadURL == url) return;
        m_lastLoadURL = url;
        StartCoroutine(DownloadAssetBundle(url, objIndex));
    }

    public void LoadModelFromURL()
    {
        string url = m_inputField.text;

        LoadModelFromURL(url, 2);

        SendShelfActionMessage(action: WebShelfAction.LOADMODEL, objIndex: 2, url: url);
    }

    #endregion LoadModelFromURL

    #region SendMessage
    /// <summary>
    /// Custom MessageはseatIndexを指定してユニキャストできる仕様
    /// </summary>
    /// <param name="message">カスタムメッセージ</param>
    /// <param name="dstIndex">宛先インデックス</param>
    public void SendWsMessage(string message, int dstIndex)
    {
        TLabSyncClient.Instalce.SendWsMessage(
            role: WebRole.GUEST, action: WebAction.CUSTOMACTION,
            seatIndex: dstIndex, customIndex: 0, custom: message);

        return;
    }

    public void SendShelfActionMessage(WebShelfAction action, int objIndex, string url = null, int dstIndex = -1)
    {
        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)action,
            objIndex = objIndex,
            url = url
        };

        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, dstIndex);
    }
    #endregion SendMessage

    #region OnMessage
    public TLabSyncShelfJson GetJson(string message)
    {
        return JsonUtility.FromJson<TLabSyncShelfJson>(message);
    }

    public void OnMessage(string message)
    {
        TLabSyncShelfJson obj = GetJson(message);

#if UNITY_EDITOR
        Debug.Log(thisName + "OnMessage - " + message);
#endif

        switch (obj.action)
        {
            case (int)WebShelfAction.LOADMODEL:
                LoadModelFromURL(obj.url, obj.objIndex);
                break;
            case (int)WebShelfAction.TAKEOUT:
                TakeOutFromOutside(obj.objIndex);
                break;
            case (int)WebShelfAction.PUTAWAY:
                PutAwayFromOutside(obj.objIndex);
                break;
            case (int)WebShelfAction.SHARE:
                ShareFromOutside(obj.objIndex);
                break;
            case (int)WebShelfAction.COLLECT:
                CollectFromOutside(obj.objIndex);
                break;
        }
    }
    #endregion OnMessage

    public void OnGuestParticipated(int anchorIndex)
    {
        if (TLabSyncClient.Instalce.SeatIndex == 0)
        {
            // URLからロードしているオブジェクト
            if (m_lastLoadURL != "")
                SendShelfActionMessage(WebShelfAction.LOADMODEL, objIndex: 2, url: m_lastLoadURL);

            // 既にインスタンス化しているオブジェクト
            if (m_currentShareds.Count > 0)
                foreach (int sharedIndex in m_currentShareds)
                    SendShelfActionMessage(action: WebShelfAction.SHARE, objIndex: sharedIndex, dstIndex: anchorIndex);

            if (m_currentTakeOuts.Count > 0)
                foreach (int takeOutIndex in m_currentTakeOuts)
                    SendShelfActionMessage(action: WebShelfAction.TAKEOUT, objIndex: takeOutIndex, dstIndex: anchorIndex);
        }
    }

    public void OnGuestDiscconected(int anchorIndex)
    {
        // 退出したプレイヤーの席から共有オブジェクトを削除する．
        for (int i = 0; i < m_shelfObjInfos.Length; i++) StartCoroutine(FadeOut(i, anchorIndex));
    }

#if UNITY_EDITOR
    private void LoadModelTest()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadModelFromURL(m_testURL, 2);
            SendShelfActionMessage(WebShelfAction.LOADMODEL, 2, m_testURL, -1);
        }

        if (Input.GetKeyDown(KeyCode.A)) TakeOut();
        if (Input.GetKeyDown(KeyCode.S)) PutAway();
        if (Input.GetKeyDown(KeyCode.D)) Share();
        if (Input.GetKeyDown(KeyCode.F)) Collect();
        if (Input.GetKeyDown(KeyCode.W)) m_currentObjIndex = Mathf.Clamp(m_currentObjIndex + 1, 0, 3);
        if (Input.GetKeyDown(KeyCode.X)) m_currentObjIndex = Mathf.Clamp(m_currentObjIndex - 1, 0, 3);
    }
#endif

    private void Update()
    {
#if UNITY_EDITOR
        LoadModelTest();
#endif
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(TLabShelfSyncManager))]
[CanEditMultipleObjects]
public class TLabShelfSyncManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        TLabShelfSyncManager manager = target as TLabShelfSyncManager;

        serializedObject.ApplyModifiedProperties();
    }
}
#endif