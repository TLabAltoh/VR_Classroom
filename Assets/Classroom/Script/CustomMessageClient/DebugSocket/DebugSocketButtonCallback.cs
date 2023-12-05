using UnityEngine;
using TLab.XR.Interact;

namespace TLab.VRClassroom
{
    public class DebugSocketButtonCallback : MonoBehaviour
    {
        [SerializeField] private DebugSocket m_debugSocket;

        [SerializeField] private ExclusiveController m_controller;

        public void OnPress() => m_debugSocket.SyncState(m_controller.id);

#if UNITY_EDITOR
        void OnValidate()
        {
            m_controller = GetComponent<ExclusiveController>();
        }
#endif
    }
}
