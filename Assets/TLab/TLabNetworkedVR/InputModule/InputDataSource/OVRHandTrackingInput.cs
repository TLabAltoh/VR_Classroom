using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.Input;

namespace TLab.XR.Input
{
    public class OVRHandTrackingInput : InputDataSource
    {
        [System.Serializable]
        public struct Gesture
        {
            public string name;
            public List<Vector3> fingerDatas;
        }

        [Header("OVR Hand")]

        [SerializeField] private FromOVRHandDataSource m_dataSource;

        [SerializeField] private RayInteractor m_rayInteractor;

        [SerializeField] private OVRSkeleton m_skeleton;

        [SerializeField] private List<Gesture> m_gestures;

        [SerializeField] private string m_targetGestureName;

        [SerializeField] private const float THRESHOLD = 0.05f;

#if UNITY_EDITOR
        [SerializeField] private bool m_editMode = false;
#endif

        private bool m_skeltonInitialized = false;

        private bool m_fired = false;

        private List<OVRBone> m_fingerBones;

        private string DetectGesture()
        {
            string result = null;

            float currentMin = Mathf.Infinity;

            foreach (var gesture in m_gestures)
            {
                float sumDistance = 0.0f;

                bool isDiscarded = false;

                for (int i = 0; i < m_fingerBones.Count; i++)
                {
                    Vector3 currentData = m_skeleton.transform.InverseTransformPoint(m_fingerBones[i].Transform.position);
                    float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);

                    if (distance > THRESHOLD)
                    {
                        isDiscarded = true;
                        break;
                    }

                    sumDistance += distance;
                }

                if (!isDiscarded && sumDistance < currentMin)
                {
                    currentMin = sumDistance;

                    result = gesture.name;
                }
            }

            return result;
        }

#if UNITY_EDITOR
        private void SavePose()
        {
            Gesture g = new Gesture();
            g.name = "New Gesture";

            List<Vector3> data = new List<Vector3>();
            foreach (var bone in m_fingerBones)
            {
                data.Add(m_skeleton.transform.InverseTransformPoint(bone.Transform.position));
            }

            g.fingerDatas = data;
            m_gestures.Add(g);
        }
#endif

        void Start()
        {

        }

        void Update()
        {
#if UNITY_EDITOR
            if (m_editMode)
            {
                if (UnityEngine.Input.GetKeyDown(KeyCode.Space))
                {
                    SavePose();
                }
            }
#endif

            var dataAsset = m_dataSource.GetData();

            var rootPose = dataAsset.Root;
            m_rootPose = new Pose
            {
                position = rootPose.position,
                rotation = rootPose.rotation
            };

            var pointerPose = dataAsset.PointerPose;
            m_pointerPose = new Pose
            {
                position = pointerPose.position,
                rotation = pointerPose.rotation
            };

            m_pointerOrigin = m_rayInteractor.Origin;

            m_pointerEnd = m_rayInteractor.End;

            if (m_skeltonInitialized)
            {
                bool fired = DetectGesture() == m_targetGestureName;
                bool prevFired = m_fired;

                m_pressed = m_fired;

                m_onPress = false;

                m_onRelease = false;

                if (fired && !prevFired)
                {
                    m_onPress = true;
                }
                else if (!fired && prevFired)
                {
                    m_onRelease = true;
                }
            }
            else
            {
                // https://communityforums.atmeta.com/t5/Unity-VR-Development/Bones-list-is-empty/td-p/880261
                if (m_skeleton.Bones.Count > 0)
                {
                    m_fingerBones = new List<OVRBone>(m_skeleton.Bones);
                    m_skeltonInitialized = true;
                }
            }
        }
    }
}
