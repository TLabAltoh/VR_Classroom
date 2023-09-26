using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TLab.InputField;
using TLab.XR.VRGrabber;

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
    LOADGAMEOBJ
}

public class TLabShelfSyncManager : TLabShelfManager
{
    [SerializeField] public TLabInputField m_inputField;
    [SerializeField] private string m_downloadUrl;
    [SerializeField] private int m_downloadIndex;

    private string m_lastLoadURL = "";
    private AssetBundle m_assetBundle;
    private List<int> m_currentShareds = new List<int>();
    private List<int> m_currentTakeOuts = new List<int>();

    private const string thisName = "[tlabsyncshelf] ";

    public void SetServerAddr(string url)
    {
        m_downloadUrl = url;
    }

    protected override IEnumerator FadeIn(int objIndex, int anchorIndex)
    {
        // ���Ȃɂ�������Ȃ�������I��
        if (TLabSyncClient.Instalce.IsGuestExist(anchorIndex) == false) yield break;

        // �I�u�W�F�N�g�̃t�F�[�h�C��
        yield return base.FadeIn(objIndex, anchorIndex);

        // �t�F�[�h�C�������I�u�W�F�N�g�͎����̐Ȃł͂Ȃ�
        // -------> ���݂̃T�[�o��Transform�Ɠ���
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
        // �z��͈̔͊O��������X�L�b�v
        if (objIndex >= m_shelfObjInfos.Length) yield break;

        TLabShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

        // �z��ɒl�����݂��Ȃ�������X�L�b�v
        if (shelfObjInfo == null) yield break;

        GameObject instanced;
        shelfObjInfo.instanced.TryGetValue(anchorIndex, out instanced);

        // �C���X�^���X�����݂��Ȃ�������X�L�b�v
        if (instanced == null) yield break;

        // �T�[�o�[�̃L���b�V�����폜

        foreach(TLabSyncGrabbable grabbable in instanced.GetComponentsInChildren<TLabSyncGrabbable>())
        {
            grabbable.ShutdownGrabber(true);
            yield return null;
        }

        foreach(TLabSyncAnim animator in instanced.GetComponentsInChildren<TLabSyncAnim>())
        {
            animator.ShutdownAnimator(true);
            yield return null;
        }

        // �C���X�^���X�̍폜
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

    #region LoadModelFromURL
    public IEnumerator DownloadAssetBundle(string modURL, int objIndex)
    {
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

        LoadModelFromURL(url, m_downloadIndex);

        SendShelfActionMessage(action: WebShelfAction.LOADGAMEOBJ, objIndex: m_downloadIndex, url: url);
    }
    #endregion LoadModelFromURL

    #region SendMessage
    /// <summary>
    /// �w�肵�����ȂɃ��b�Z�[�W�����j�L���X�g
    /// </summary>
    /// <param name="message">�J�X�^�����b�Z�[�W</param>
    /// <param name="dstIndex">����C���f�b�N�X</param>
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
            case (int)WebShelfAction.LOADGAMEOBJ:
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
        // ���t������V�����Q���҂ɋ��L���̓������s��
        if (TLabSyncClient.Instalce.SeatIndex == 0)
        {
            // �I�Ƀ_�E�����[�h���Ă���I�u�W�F�N�g
            if (m_lastLoadURL != "")
                SendShelfActionMessage(WebShelfAction.LOADGAMEOBJ, objIndex: m_downloadIndex, url: m_lastLoadURL);

            // ���k���̃I�u�W�F�N�g
            if (m_currentShareds.Count > 0)
                foreach (int sharedIndex in m_currentShareds)
                    SendShelfActionMessage(action: WebShelfAction.SHARE, objIndex: sharedIndex, dstIndex: anchorIndex);

            // ���t���̃I�u�W�F�N�g
            if (m_currentTakeOuts.Count > 0)
                foreach (int takeOutIndex in m_currentTakeOuts)
                    SendShelfActionMessage(action: WebShelfAction.TAKEOUT, objIndex: takeOutIndex, dstIndex: anchorIndex);
        }

        // �e�v���C���[���ŁC�V�����Q���҂̐Ȃɋ��L���ꂽ�I�u�W�F�N�g�𓯊�����
        if (m_currentShareds.Count > 0)
            foreach (int sharedIndex in m_currentShareds)
                FadeIn(sharedIndex, anchorIndex);
    }

    public void OnGuestDiscconected(int anchorIndex)
    {
        // �ޏo�����v���C���[�̐Ȃ��狤�L�I�u�W�F�N�g���������D
        for (int objIndex = 0; objIndex < m_shelfObjInfos.Length; objIndex++)
            StartCoroutine(FadeOut(objIndex, anchorIndex));
    }

#if UNITY_EDITOR
    private void DownloadGameObjFromKeyborad()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadModelFromURL(m_downloadUrl, m_downloadIndex);
            SendShelfActionMessage(WebShelfAction.LOADGAMEOBJ, m_downloadIndex, m_downloadUrl, -1);
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
        DownloadGameObjFromKeyborad();
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