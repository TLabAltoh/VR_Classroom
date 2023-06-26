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
    takeOut,
    putAway,
    share,
    collect,
    loadModel
}

public class TLabShelfSyncManager : TLabShelfManager
{
    [SerializeField] public TLabInputField m_inputField;
    private string m_lastLoadURL = "";
    private AssetBundle m_assetBundle;
    private List<int> m_currentShareds = new List<int>();

#if UNITY_EDITOR
    [SerializeField] private string m_testURL;

    public void SetServerAddr(string url)
    {
        m_testURL = url;
    }
#endif

    protected override IEnumerator FadeIn(int objIndex, int anchorIndex)
    {
        // ���Ȃɂ�������Ȃ�������X�L�b�v
        if (TLabSyncClient.Instalce.IsGuestExist(anchorIndex) == false) yield break;

        // �����̑�łȂ��I�u�W�F�N�g�������݂̃T�[�o�[��Transform�Ƃ̓������s��
        bool reloadWorldData = TLabSyncClient.Instalce.SeatIndex != anchorIndex;

        yield return base.FadeIn(objIndex, anchorIndex);
        TLabSyncClient.Instalce.ForceReflesh(reloadWorldData);
        yield break;
    }

    protected override IEnumerator FadeOut(int objIndex, int anchorIndex)
    {
        bool reloadWorldData = TLabSyncClient.Instalce.SeatIndex != anchorIndex;

        yield return base.FadeOut(objIndex, anchorIndex);
        TLabSyncClient.Instalce.ForceReflesh(reloadWorldData);
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

    #region FromOutside

    private void PutAwayFromOutside(int objIndex)
    {
        StartCoroutine(FadeOut(objIndex, 0));
    }

    private void TakeOutFromOutside(int objIndex)
    {
        StartCoroutine(FadeIn(objIndex, 0));
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
        Debug.Log("[tlabsyncshelf] Start Load Asset");
#endif

        if (m_assetBundle != null) m_assetBundle.Unload(false);

        var request = UnityWebRequestAssetBundle.GetAssetBundle(modURL);
        yield return request.SendWebRequest();

        // Handle error
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError ||
            request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.LogError("[tlabsyncshelf] " + request.error);
            yield break;
        }

        var handler = request.downloadHandler as DownloadHandlerAssetBundle;
        m_assetBundle = handler.assetBundle;

#if UNITY_EDITOR
        Debug.Log("[tlabsyncshelf] Finish Load Asset");
#endif

        AssetBundleRequest assetLoadRequest = m_assetBundle.LoadAllAssetsAsync<GameObject>();
        yield return assetLoadRequest;

        GameObject prefab = assetLoadRequest.allAssets[0] as GameObject;

        m_shelfObjInfos[objIndex].obj = prefab;
    }

    /// <summary>
    /// - InputField�ɓ��͂���URL����C3D���f��(AssetBundle�`��)���_�E�����[�h����D
    /// </summary>
    public void LoadModelFromURL(string url, int objIndex)
    {
        if (m_lastLoadURL == url) return;
        m_lastLoadURL = url;
        StartCoroutine(DownloadAssetBundle(url, objIndex));
    }

    /// <summary>
    /// UI����LoadModelFromURL(url, objIndex)���Ăяo��
    /// </summary>
    public void LoadModelFromURL()
    {
        LoadModelFromURL(m_inputField.text, 2);

        // ���v���C���[�ɂ��I�ւ̒ǉ���ʒm
        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action = (int)WebShelfAction.loadModel,
            url = m_inputField.text,
            objIndex = 2
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    #endregion LoadModelFromURL

    /// <summary>
    /// // Custom Message��seatIndex���w�肵�ă��j�L���X�g�ł���d�l
    /// </summary>
    /// <param name="message">�J�X�^�����b�Z�[�W</param>
    /// <param name="anchorIndex">����C���f�b�N�X</param>
    public void SendWsMessage(string message, int anchorIndex)
    {
        TLabSyncJson obj = new TLabSyncJson
        {
            role        = (int)WebRole.guest,
            action      = (int)WebAction.customAction,
            seatIndex   = anchorIndex,
            customIndex = 0,
            custom      = message
        };
        string json = JsonUtility.ToJson(obj);

        TLabSyncClient.Instalce.SendWsMessage(json);

        return;
    }

    /// <summary>
    /// �J�X�^�����b�Z�[�W��M���̃R�[���o�b�N����
    /// </summary>
    /// <param name="message"></param>
    public void OnMessage(string message)
    {
        TLabSyncShelfJson obj = JsonUtility.FromJson<TLabSyncShelfJson>(message);

#if UNITY_EDITOR
        Debug.Log("[tlabsyncshelf] OnMessage - " + message);
#endif

        if (obj.action == (int)WebShelfAction.loadModel)    LoadModelFromURL(obj.url, obj.objIndex);
        else if (obj.action == (int)WebShelfAction.takeOut) TakeOutFromOutside(obj.objIndex);
        else if (obj.action == (int)WebShelfAction.putAway) PutAwayFromOutside(obj.objIndex);
        else if (obj.action == (int)WebShelfAction.share)   ShareFromOutside(obj.objIndex);
        else if (obj.action == (int)WebShelfAction.collect) CollectFromOutside(obj.objIndex);

        return;
    }

    /// <summary>
    /// - ���[���ɐV�����Q�������v���C���[�ɁC�������I�u�W�F�N�g�������Ă��邱�Ƃ�ʒm����
    /// - ���X�g�̃I�u�W�F�N�g�����ׂċ��L
    /// - ���݃��[�h���Ă���I�u�W�F�N�g��������ʒm����
    /// </summary>
    /// <param name="anchorIndex">�Q�������v���C���[�̃C���f�b�N�X</param>
    public void OnGuestParticipated(int anchorIndex)
    {
        {
            if (m_currentShareds.Count == 0) return;

            foreach (int sharedIndex in m_currentShareds)
            {
                TLabSyncShelfJson obj = new TLabSyncShelfJson
                {
                    action = (int)WebShelfAction.share,
                    objIndex = sharedIndex
                };
                string json = JsonUtility.ToJson(obj);
                SendWsMessage(json, anchorIndex);
            }
        }

        {
            if(TLabSyncClient.Instalce.SeatIndex == 0 || m_lastLoadURL != "")
            {
                TLabSyncShelfJson obj = new TLabSyncShelfJson
                {
                    action = (int)WebShelfAction.loadModel,
                    url = m_lastLoadURL,
                    objIndex = 2
                };
                string json = JsonUtility.ToJson(obj);
                SendWsMessage(json, -1);
            }
        }
    }

    /// <summary>
    /// - �ޏo�����v���C���[�̍��Ȃ��狤�L�I�u�W�F�N�g���폜����D
    /// </summary>
    /// <param name="anchorIndex">�ޏo�����v���C���[�̃C���f�b�N�X</param>
    public void OnGuestDiscconected(int anchorIndex)
    {
        for(int i = 0; i < m_shelfObjInfos.Length; i++) FadeOut(i, anchorIndex);
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

        if (GUILayout.Button("Initialize Shelf Obj"))
        {
            // 
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif