using UnityEngine;

namespace TLab.XR.Network
{
    public class SeatIdentifier : MonoBehaviour
    {
#if UNITY_EDITOR
        [Header("Debug")] [SerializeField]
#endif

        private int m_seatIndex = SyncClient.NOT_REGISTED;

        public int seatIndex { get => m_seatIndex; set => m_seatIndex = value; }
    }
}
