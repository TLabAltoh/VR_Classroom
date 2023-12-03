using UnityEngine;
using TMPro;
using TLab.XR.Network;
using TLab.XR.Interact;

namespace TLab.VRClassroom
{
    [System.Serializable]
    public class DebugSocketJson
    {
        public int action;
        public string id;
        public bool useRigidbody = false;
        public bool useGravity = false;
        public bool rbAllocated = false;
        public WebVector3 position = null;
        public WebVector4 rotation = null;
        public int grabbIndex = ExclusiveController.FREE;
    }

    public enum WebDebugAction
    {
        SYNC_STATE,
        SWITCH_DEBUG_TARGET
    }

    public class DebugSocket : MonoBehaviour
    {
        [Header("Custom Message Index")]

        [SerializeField] private int m_customIndex = 1;

        [Header("Debug Text")]

        [SerializeField] private TextMeshProUGUI m_id;

        [SerializeField] private TextMeshProUGUI m_useRigidbody;

        [SerializeField] private TextMeshProUGUI m_useGravity;

        [SerializeField] private TextMeshProUGUI m_rbAllocated;

        [SerializeField] private TextMeshProUGUI m_position;

        [SerializeField] private TextMeshProUGUI m_rotation;

        [SerializeField] private TextMeshProUGUI m_grabbIndex;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public const int BROADCAST = -1;

        private const float INTERVAL = 0.5f;

        private const float ZERO_FRAME = 0.0f;

        private float m_elapsed = 0.0f;

        private ExclusiveController m_controller;

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
                seatIndex: dstIndex, customIndex: m_customIndex, custom: message);
        }

        public void SendDebugActionMessage(WebDebugAction action, ExclusiveController controller = null, int dstIndex = BROADCAST)
        {
            var obj = new DebugSocketJson
            {
                action = (int)action,
                id = controller?.id,
                useRigidbody = controller.useRigidbody,
                useGravity = controller.useGravity,
                rbAllocated = controller.rbAllocated,
                position = new WebVector3 { x = controller.transform.position.x, y = controller.transform.position.y, z = controller.transform.position.z },
                rotation = new WebVector4 { x = controller.transform.rotation.x, y = controller.transform.rotation.y, z = controller.transform.rotation.z, w = controller.transform.rotation.w },
                grabbIndex = controller.grabbedIndex
            };

            string json = JsonUtility.ToJson(obj);
            SendWsMessage(json, dstIndex);
        }

        #endregion SEND_MESSAGE

        #region ON_MESSAGE

        public DebugSocketJson GetJson(string message) => JsonUtility.FromJson<DebugSocketJson>(message);

        public void OnMessage(string message)
        {
            var obj = GetJson(message);

#if UNITY_EDITOR
            Debug.Log(THIS_NAME + "OnMessage - " + message);
#endif

            switch (obj.action)
            {
                case (int)WebDebugAction.SWITCH_DEBUG_TARGET:

                    // 

                    m_controller = ExclusiveController.GetById(obj.id);

                    break;
                case (int)WebDebugAction.SYNC_STATE:

                    // Debug info received.

                    m_id.text = obj.id.ToString();
                    m_useRigidbody.text = obj.useRigidbody.ToString();
                    m_useGravity.text = obj.useGravity.ToString();
                    m_rbAllocated.text = obj.rbAllocated.ToString();
                    m_position.text = obj.position.x.ToString("0.00") + ", " + obj.position.y.ToString("0.00") + ", " + obj.position.z.ToString("0.00");
                    m_rotation.text = obj.rotation.x.ToString("0.00") + ", " + obj.rotation.y.ToString("0.00") + ", " + obj.rotation.z.ToString("0.00") + ", " + obj.rotation.w.ToString("0.00"); 
                    m_grabbIndex.text = obj.grabbIndex.ToString();
                    break;
            }
        }

        #endregion ON_MESSAGE

        public void OnGuestParticipated(int anchorIndex) { }

        public void OnGuestDiscconected(int anchorIndex) { }

        public void SyncState(string id) => SendDebugActionMessage(WebDebugAction.SWITCH_DEBUG_TARGET, ExclusiveController.GetById(id));

        void Update()
        {
            if (m_controller != null)
            {
                if (m_elapsed > INTERVAL)
                {
                    SendDebugActionMessage(WebDebugAction.SYNC_STATE, m_controller);

                    m_elapsed = ZERO_FRAME;
                }

                m_elapsed += Time.deltaTime;
            }
        }
    }
}
