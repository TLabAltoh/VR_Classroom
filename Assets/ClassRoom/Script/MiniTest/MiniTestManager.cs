using UnityEngine;
using TLab.XR.VRGrabber;

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
        [Header("Debug")]
        [SerializeField] private int[] m_scores;

        private const string THIS_NAME = "[tlabminitest] ";

        public static MiniTestManager Instance;

        public int GetScore(int index)
        {
            return m_scores[index];
        }

        public void RegistScore(int score)
        {
            m_scores[SyncClient.Instance.SeatIndex] = score;

            SendMiniTestActionMessage(
                WebMiniTestAction.REGISTRATION,
                score: score, seatIndex: SyncClient.Instance.SeatIndex, dstIndex: -1);
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
            m_scores[SyncClient.Instance.SeatIndex] = score;

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
                role: WebRole.GUEST, action: WebAction.CUSTOMACTION, seatIndex: anchorIndex,
                customIndex: 1, custom: customJson);
        }

        void Awake()
        {
            Instance = this;
        }

        void Start()
        {
            m_scores = new int[SyncClient.Instance.SeatLength];
        }
    }
}