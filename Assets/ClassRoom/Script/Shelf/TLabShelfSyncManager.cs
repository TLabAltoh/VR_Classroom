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
    private List<int> m_currentShareds  = new List<int>();
    private List<int> m_currentTakeOuts = new List<int>();

    private const string thisName = "[tlabsyncshelf] ";

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

        // �I�u�W�F�N�g�̃t�F�[�h�C��
        yield return base.FadeIn(objIndex, anchorIndex);

        // �����̑�łȂ��I�u�W�F�N�g�̃C���X�^���X���������݂̃T�[�o�[��Transform�Ƃ̓������s��
        bool reloadWorldData = TLabSyncClient.Instalce.SeatIndex != anchorIndex;
        string objName = m_shelfObjInfos[objIndex].instanced[anchorIndex].name;

        if (reloadWorldData) TLabSyncClient.Instalce.UniReflesh(objName);

        yield break;
    }

    protected override IEnumerator FadeOut(int objIndex, int anchorIndex)
    {
        bool reloadWorldData = TLabSyncClient.Instalce.SeatIndex != anchorIndex;
        string objName = m_shelfObjInfos[objIndex].instanced[anchorIndex].name;

        // �I�u�W�F�N�g�̃t�F�[�h�A�E�g
        yield return base.FadeOut(objIndex, anchorIndex);

        if (reloadWorldData) TLabSyncClient.Instalce.UniReflesh(objName);

        yield break;
    }

    public override void TakeOut()
    {
        base.TakeOut();

        m_currentTakeOuts.Add(m_currentObjIndex);

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action      = (int)WebShelfAction.takeOut,
            objIndex    = m_currentObjIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    public override void PutAway()
    {
        base.PutAway();

        m_currentTakeOuts.Remove(m_currentObjIndex);

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action      = (int)WebShelfAction.putAway,
            objIndex    = m_currentObjIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    public override void Share()
    {
        base.Share();

        m_currentShareds.Add(m_currentObjIndex);

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action      = (int)WebShelfAction.share,
            objIndex    = m_currentObjIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
    }

    public override void Collect()
    {
        base.Collect();

        m_currentShareds.Remove(m_currentObjIndex);

        TLabSyncShelfJson obj = new TLabSyncShelfJson
        {
            action      = (int)WebShelfAction.collect,
            objIndex    = m_currentObjIndex
        };
        string json = JsonUtility.ToJson(obj);
        SendWsMessage(json, -1);
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
        if (request.result == UnityWebRequest.Result.ConnectionError ||
            request.result == UnityWebRequest.Result.ProtocolError ||
            request.result == UnityWebRequest.Result.DataProcessingError)
        {
            Debug.LogError(thisName + request.error);
            yield break;
        }

        var handler     = request.downloadHandler as DownloadHandlerAssetBundle;
        m_assetBundle   = handler.assetBundle;

#if UNITY_EDITOR
        Debug.Log(thisName + "Finish Load Asset");
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
            role        = (int)WebRole.GUEST,
            action      = (int)WebAction.CUSTOMACTION,
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
        Debug.Log(thisName + "OnMessage - " + message);
#endif
        switch (obj.action)
        {
            case (int)WebShelfAction.loadModel:
                LoadModelFromURL(obj.url, obj.objIndex);
                break;
            case (int)WebShelfAction.takeOut:
                TakeOutFromOutside(obj.objIndex);
                break;
            case (int)WebShelfAction.putAway:
                PutAwayFromOutside(obj.objIndex);
                break;
            case (int)WebShelfAction.share:
                ShareFromOutside(obj.objIndex);
                break;
            case (int)WebShelfAction.collect:
                CollectFromOutside(obj.objIndex);
                break;
        }
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
            // URL���烍�[�h���Ă���I�u�W�F�N�g

            if (TLabSyncClient.Instalce.SeatIndex == 0 && m_lastLoadURL != "")
            {
                TLabSyncShelfJson obj = new TLabSyncShelfJson
                {
                    action      = (int)WebShelfAction.loadModel,
                    url         = m_lastLoadURL,
                    objIndex    = 2
                };
                string json = JsonUtility.ToJson(obj);
                SendWsMessage(json, -1);
            }
        }

        {
            // ���ɃC���X�^���X�����Ă���I�u�W�F�N�g

            if (m_currentShareds.Count > 0)
            {
                foreach (int sharedIndex in m_currentShareds)
                {
                    TLabSyncShelfJson obj = new TLabSyncShelfJson
                    {
                        action      = (int)WebShelfAction.share,
                        objIndex    = sharedIndex
                    };
                    string json = JsonUtility.ToJson(obj);
                    SendWsMessage(json, anchorIndex);
                }
            }

            if (m_currentTakeOuts.Count > 0)
            {
                foreach (int takeOutIndex in m_currentTakeOuts)
                {
                    TLabSyncShelfJson obj = new TLabSyncShelfJson
                    {
                        action      = (int)WebShelfAction.takeOut,
                        objIndex    = takeOutIndex
                    };
                    string json = JsonUtility.ToJson(obj);
                    SendWsMessage(json, anchorIndex);
                }
            }
        }
    }

    /// <summary>
    /// - �ޏo�����v���C���[�̍��Ȃ��狤�L�I�u�W�F�N�g���폜����D
    /// </summary>
    /// <param name="anchorIndex">�ޏo�����v���C���[�̃C���f�b�N�X</param>
    public void OnGuestDiscconected(int anchorIndex)
    {
        for (int i = 0; i < m_shelfObjInfos.Length; i++) StartCoroutine(FadeOut(i, anchorIndex));
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.Space))
        {
            LoadModelFromURL(m_testURL, 2);

            TLabSyncShelfJson obj = new TLabSyncShelfJson
            {
                action      = (int)WebShelfAction.loadModel,
                url         = m_testURL,
                objIndex    = 2
            };
            string json = JsonUtility.ToJson(obj);
            SendWsMessage(json, -1);
        }

        if (Input.GetKeyDown(KeyCode.A)) TakeOut();
        if (Input.GetKeyDown(KeyCode.S)) PutAway();
        if (Input.GetKeyDown(KeyCode.D)) Share();
        if (Input.GetKeyDown(KeyCode.F)) Collect();
        if (Input.GetKeyDown(KeyCode.W)) m_currentObjIndex = Mathf.Clamp(m_currentObjIndex + 1, 0, 3);
        if (Input.GetKeyDown(KeyCode.X)) m_currentObjIndex = Mathf.Clamp(m_currentObjIndex - 1, 0, 3);
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