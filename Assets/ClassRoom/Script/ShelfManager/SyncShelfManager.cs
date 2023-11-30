using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TLab.InputField;
using TLab.XR.Network;
using TLab.XR.Interact;

namespace TLab.VRClassroom
{
    [System.Serializable]
    public class SyncShelfJson
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

    public class SyncShelfManager : ShelfManager
    {
        [SerializeField] public TLabInputField m_inputField;
        [SerializeField] private string m_downloadUrl;
        [SerializeField] private int m_downloadIndex;

        private string m_lastLoadURL = "";
        private AssetBundle m_assetBundle;
        private List<int> m_currentShareds = new List<int>();
        private List<int> m_currentTakeOuts = new List<int>();

        private const string THIS_NAME = "[tlabsyncshelf] ";

        public void SetServerAddr(string url)
        {
            m_downloadUrl = url;
        }

        protected override IEnumerator FadeIn(int objIndex, int anchorIndex)
        {
            // 座席にだれもいなかったら終了
            if (!SyncClient.Instance.IsGuestExist(anchorIndex))
            {
                yield break;
            }

            // オブジェクトのフェードイン
            yield return base.FadeIn(objIndex, anchorIndex);

            // フェードインしたオブジェクトは自分の席ではない
            // -------> 現在のサーバのTransformと同期
            bool reloadWorldData = SyncClient.Instance.seatIndex != anchorIndex;

            if (reloadWorldData)
            {
                string objName = m_shelfObjInfos[objIndex].instanced[anchorIndex].name;
                SyncClient.Instance.UniReflesh(objName);
            }

            Debug.Log(THIS_NAME + "fade in");

            yield break;
        }

        protected override IEnumerator FadeOut(int objIndex, int anchorIndex)
        {
            // 配列の範囲外だったらスキップ
            if (objIndex >= m_shelfObjInfos.Length)
            {
                yield break;
            }

            ShelfObjInfo shelfObjInfo = m_shelfObjInfos[objIndex];

            // 配列に値が存在しなかったらスキップ
            if (shelfObjInfo == null)
            {
                yield break;
            }

            GameObject instanced;
            shelfObjInfo.instanced.TryGetValue(anchorIndex, out instanced);

            // インスタンスが存在しなかったらスキップ
            if (instanced == null)
            {
                yield break;
            }

            // サーバーのキャッシュを削除

            foreach (Grabbable grabbable in instanced.GetComponentsInChildren<Grabbable>())
            {
                grabbable.Shutdown(true);
                yield return null;
            }

            foreach (SyncAnimator animator in instanced.GetComponentsInChildren<SyncAnimator>())
            {
                animator.Shutdown(true);
                yield return null;
            }

            // インスタンスの削除
            shelfObjInfo.instanced.Remove(anchorIndex);
            Destroy(instanced);

            Debug.Log(THIS_NAME + "fade out");

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

            for (int i = 1; i < m_anchors.Length; i++)
            {
                StartCoroutine(FadeIn(objIndex, i));
            }
        }

        private void CollectFromOutside(int objIndex)
        {
            m_currentShareds.Remove(objIndex);

            for (int i = 1; i < m_anchors.Length; i++)
            {
                StartCoroutine(FadeOut(objIndex, i));
            }
        }

        #region LOAD_MODEL_FROM_URL
        public IEnumerator DownloadAssetBundle(string modURL, int objIndex)
        {
            if (m_assetBundle != null)
            {
                m_assetBundle.Unload(false);
            }

            var request = UnityWebRequestAssetBundle.GetAssetBundle(modURL);
            yield return request.SendWebRequest();

            // Handle error
            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError || request.result == UnityWebRequest.Result.DataProcessingError)
            {
                Debug.LogError(THIS_NAME + request.error);
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
            if (m_lastLoadURL == url)
            {
                return;
            }

            m_lastLoadURL = url;
            StartCoroutine(DownloadAssetBundle(url, objIndex));
        }

        public void LoadModelFromURL()
        {
            string url = m_inputField.text;

            LoadModelFromURL(url, m_downloadIndex);

            SendShelfActionMessage(action: WebShelfAction.LOADGAMEOBJ, objIndex: m_downloadIndex, url: url);
        }
        #endregion LOAD_MODEL_FROM_URL

        #region SEND_MESSAGE
        /// <summary>
        /// 指定した座席にメッセージをユニキャスト
        /// </summary>
        /// <param name="message">カスタムメッセージ</param>
        /// <param name="dstIndex">宛先インデックス</param>
        public void SendWsMessage(string message, int dstIndex)
        {
            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST, action: WebAction.CUSTOMACTION,
                seatIndex: dstIndex, customIndex: 0, custom: message);

            return;
        }

        public void SendShelfActionMessage(WebShelfAction action, int objIndex, string url = null, int dstIndex = -1)
        {
            SyncShelfJson obj = new SyncShelfJson
            {
                action = (int)action,
                objIndex = objIndex,
                url = url
            };

            string json = JsonUtility.ToJson(obj);
            SendWsMessage(json, dstIndex);
        }
        #endregion SEND_MESSAGE

        #region ON_MESSAGE
        public SyncShelfJson GetJson(string message)
        {
            return JsonUtility.FromJson<SyncShelfJson>(message);
        }

        public void OnMessage(string message)
        {
            SyncShelfJson obj = GetJson(message);

#if UNITY_EDITOR
            Debug.Log(THIS_NAME + "OnMessage - " + message);
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
        #endregion ON_MESSAGE

        public void OnGuestParticipated(int anchorIndex)
        {
            // 教師側から新しい参加者に共有情報の同期を行う
            if (SyncClient.Instance.seatIndex == SyncClient.HOST_INDEX)
            {
                // 棚にダウンロードしているオブジェクト
                if (m_lastLoadURL != "")
                {
                    SendShelfActionMessage(WebShelfAction.LOADGAMEOBJ, objIndex: m_downloadIndex, url: m_lastLoadURL);
                }

                // 生徒側のオブジェクト
                if (m_currentShareds.Count > 0)
                {
                    foreach (int sharedIndex in m_currentShareds)
                    {
                        SendShelfActionMessage(action: WebShelfAction.SHARE, objIndex: sharedIndex, dstIndex: anchorIndex);
                    }
                }

                // 教師側のオブジェクト
                if (m_currentTakeOuts.Count > 0)
                {
                    foreach (int takeOutIndex in m_currentTakeOuts)
                    {
                        SendShelfActionMessage(action: WebShelfAction.TAKEOUT, objIndex: takeOutIndex, dstIndex: anchorIndex);
                    }
                }
            }

            // 各プレイヤー側で，新しい参加者の席に共有されたオブジェクトを同期する
            if (m_currentShareds.Count > 0)
            {
                foreach (int sharedIndex in m_currentShareds)
                {
                    FadeIn(sharedIndex, anchorIndex);
                }
            }
        }

        public void OnGuestDiscconected(int anchorIndex)
        {
            // 退出したプレイヤーの席から共有オブジェクトを回収する．
            for (int objIndex = 0; objIndex < m_shelfObjInfos.Length; objIndex++)
            {
                StartCoroutine(FadeOut(objIndex, anchorIndex));
            }
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
}