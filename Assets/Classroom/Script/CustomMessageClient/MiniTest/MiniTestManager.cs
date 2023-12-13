using UnityEngine;
using TLab.XR.Network;

namespace TLab.VRClassroom
{
    [System.Serializable]
    public class MiniTestJson
    {
        public int action;
        public int score;
        public int seatIndex = -1;
    }

    public enum WebMiniTestAction
    {
        REGISTRATION,
    }

    public class MiniTestManager : MonoBehaviour
    {
        [Header("Custom Message Index")]
        [SerializeField] private int m_customIndex = 1;

        [Header("Debug Score")]
        [SerializeField] private int[] m_scores;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public static int BROADCAST = -1;

        public static MiniTestManager Instance;

        public int GetScore(int index) => m_scores[index];

        public void RegistScore(int score)
        {
            m_scores[SyncClient.Instance.seatIndex] = score;

            SendMiniTestActionMessage(
                WebMiniTestAction.REGISTRATION,
                score: score,
                seatIndex: SyncClient.Instance.seatIndex,
                dstIndex: BROADCAST);
        }

        public void OnMessage(string message)
        {
            var obj = JsonUtility.FromJson<MiniTestJson>(message);

#if UNITY_EDITOR
            Debug.Log(THIS_NAME + "OnMessage - " + message);
#endif

            switch (obj.action)
            {
                case (int)WebMiniTestAction.REGISTRATION:
                    m_scores[obj.seatIndex] = obj.score;
                    break;
            }
        }

        public void OnGuestParticipated(int anchorIndex) { }

        public void OnGuestDiscconected(int anchorIndex) { }

        public void SendMiniTestActionMessage(WebMiniTestAction action, int score = 0, int seatIndex = -1, int dstIndex = -1)
        {
            m_scores[SyncClient.Instance.seatIndex] = score;

            var obj = new MiniTestJson
            {
                action = (int)action,
                score = score,
                seatIndex = seatIndex
            };
            SendWsMessage(JsonUtility.ToJson(obj), dstIndex);
        }

        public void SendWsMessage(string customJson, int anchorIndex)
        {
            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.CUSTOMACTION,
                seatIndex: anchorIndex,
                customIndex: m_customIndex,
                custom: customJson);
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            m_scores = new int[SyncClient.Instance.seatLength];
        }
    }
}