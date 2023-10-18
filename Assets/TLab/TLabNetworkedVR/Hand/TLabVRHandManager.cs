using UnityEngine;
using UnityEngine.EventSystems;

namespace TLab.XR.VRGrabber
{
    public class TLabVRHandManager : MonoBehaviour
    {
#if UNITY_EDITOR
        public OVRProjectConfig ProjectConfig => m_projectConfig;
#endif

        public TLabOVRInputModule InputModule => m_inputModule;

        public TLabVRHand VRControllerHandRight => m_vrControllerHandRight;

        public TLabVRHand VRControllerHandLeft => m_vrControllerHandLeft;

        public TLabVRTrackingHand VRTrackingHandRight => m_vrTrackingHandRight;

        public TLabVRTrackingHand VRTrackingHandLeft => m_vrTrackingHandLeft;

        public GameObject VRHandRight => m_vrHandRight;

        public GameObject VRHandLeft => m_vrHandLeft;

        public GameObject VRControllerRight => m_vrControllerRight;

        public GameObject VRControllerLeft => m_vrControllerLeft;

        public GameObject VRCustomHandRight => m_vrCustomHandRight;

        public GameObject VRCustomHandLeft => m_vrCustomHandLeft;

#if UNITY_EDITOR
        [SerializeField] private OVRProjectConfig m_projectConfig;
#endif

        [SerializeField] private TLabOVRInputModule m_inputModule;

        [SerializeField] private TLabVRHand m_vrControllerHandRight;
        [SerializeField] private TLabVRHand m_vrControllerHandLeft;
        [SerializeField] private TLabVRTrackingHand m_vrTrackingHandRight;
        [SerializeField] private TLabVRTrackingHand m_vrTrackingHandLeft;

        [SerializeField] private GameObject m_vrHandRight;
        [SerializeField] private GameObject m_vrHandLeft;
        [SerializeField] private GameObject m_vrControllerRight;
        [SerializeField] private GameObject m_vrControllerLeft;
        [SerializeField] private GameObject m_vrCustomHandRight;
        [SerializeField] private GameObject m_vrCustomHandLeft;

        private void Start()
        {
            if (m_vrTrackingHandLeft.enabled || m_vrTrackingHandRight)
            {
                m_inputModule.rayTransformLeft = m_vrTrackingHandLeft.PointerPose;
                m_inputModule.rayTransformRight = m_vrTrackingHandRight.PointerPose;
            }
        }
    }
}
