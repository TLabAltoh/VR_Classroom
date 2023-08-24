using UnityEngine;
using UnityEngine.EventSystems;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.XR.VRGrabber
{
    public class TLabVRHandManager : MonoBehaviour
    {
        #region
#if UNITY_EDITOR
        public OVRProjectConfig ProjectConfig
        {
            get
            {
                return m_projectConfig;
            }
        }
#endif

        public TLabOVRInputModule InputModule
        {
            get
            {
                return m_inputModule;
            }
        }

        public TLabVRHand VRControllerHandRight
        {
            get
            {
                return m_vrControllerHandRight;
            }
        }

        public TLabVRHand VRControllerHandLeft
        {
            get
            {
                return m_vrControllerHandLeft;
            }
        }

        public TLabVRTrackingHand VRTrackingHandRight
        {
            get
            {
                return m_vrTrackingHandRight;
            }
        }

        public TLabVRTrackingHand VRTrackingHandLeft
        {
            get
            {
                return m_vrTrackingHandLeft;
            }
        }

        public GameObject VRHandRight
        {
            get
            {
                return m_vrHandRight;
            }
        }

        public GameObject VRHandLeft
        {
            get
            {
                return m_vrHandLeft;
            }
        }

        public GameObject VRControllerRight
        {
            get
            {
                return m_vrControllerRight;
            }
        }

        public GameObject VRControllerLeft
        {
            get
            {
                return m_vrControllerLeft;
            }
        }

        public GameObject VRCustomHandRight
        {
            get
            {
                return m_vrCustomHandRight;
            }
        }

        public GameObject VRCustomHandLeft
        {
            get
            {
                return m_vrCustomHandLeft;
            }
        }
        #endregion

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

#if UNITY_EDITOR
    [CustomEditor(typeof(TLabVRHandManager))]
    [CanEditMultipleObjects]
    public class TLabVRHandManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            TLabVRHandManager manager = target as TLabVRHandManager;

            bool updated = false;
            bool controller = false;
            bool customHand = false;
            bool hand = false;
            Transform right = null;
            Transform left = null;

            if (GUILayout.Button("Switch Hand"))
            {
                right = manager.VRControllerRight.transform;
                left = manager.VRControllerLeft.transform;
                hand = true;
                updated = true;
            }

            if (GUILayout.Button("Switch Custom Hand"))
            {
                right = manager.VRCustomHandRight.transform;
                left = manager.VRCustomHandLeft.transform;
                customHand = true;
                updated = true;
            }

            if (GUILayout.Button("Switch Controller"))
            {
                right = manager.VRControllerRight.transform;
                left = manager.VRControllerLeft.transform;
                controller = true;
                updated = true;
            }

            if (updated == true)
            {
                manager.VRControllerHandRight.enabled = controller;
                manager.VRControllerHandLeft.enabled = controller;
                manager.VRTrackingHandRight.enabled = hand || customHand;
                manager.VRTrackingHandLeft.enabled = hand || customHand;

                if (hand || customHand) manager.ProjectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
                else manager.ProjectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersOnly;

                manager.InputModule.rayTransformRight = right;
                manager.InputModule.rayTransformLeft = left;
                manager.VRControllerRight.SetActive(controller);
                manager.VRControllerLeft.SetActive(controller);
                manager.VRCustomHandRight.SetActive(customHand);
                manager.VRCustomHandLeft.SetActive(customHand);
                manager.VRHandRight.SetActive(hand);
                manager.VRHandLeft.SetActive(hand);

                EditorUtility.SetDirty(manager.InputModule);

                EditorUtility.SetDirty(manager.ProjectConfig);

                EditorUtility.SetDirty(manager.VRControllerHandRight);
                EditorUtility.SetDirty(manager.VRControllerHandLeft);
                EditorUtility.SetDirty(manager.VRTrackingHandRight);
                EditorUtility.SetDirty(manager.VRTrackingHandLeft);

                EditorUtility.SetDirty(manager.VRControllerRight);
                EditorUtility.SetDirty(manager.VRControllerLeft);
                EditorUtility.SetDirty(manager.VRCustomHandRight);
                EditorUtility.SetDirty(manager.VRCustomHandLeft);
                EditorUtility.SetDirty(manager.VRHandRight);
                EditorUtility.SetDirty(manager.VRHandLeft);
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
