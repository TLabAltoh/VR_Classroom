using UnityEngine;
using TLab.XR.Input;

namespace TLab.XR
{
    public class TLabXRHand : MonoBehaviour
    {
        [SerializeField] private InputDataSource m_inputDataSource;

        private Quaternion m_prevHandRotation;
        private Quaternion m_handRotation;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        // Input Data Source

        public InputDataSource inputDataSource => m_inputDataSource;

        // Gesture

        public string currentGesture => m_inputDataSource.currentGesture;

        // Pose

        public Pose pointerPose => m_inputDataSource.pointerPose;

        public Pose rootPose => m_inputDataSource.rootPose;

        public Vector3 angulerVelocity
        {
            get
            {
                // https://nekojara.city/unity-object-angular-velocity
                var diffRotation = Quaternion.Inverse(m_prevHandRotation) * m_handRotation;

                diffRotation.ToAngleAxis(out var angle, out var axis);

                return m_handRotation * axis * (angle / Time.deltaTime);
            }
        }

        // Ray Interactor's Input

        public float pressStrength => m_inputDataSource.pressStrength;

        public bool pressed => m_inputDataSource.pressed;

        public bool onPress => m_inputDataSource.onPress;

        public bool onRelease => m_inputDataSource.onRelease;

        // Grab Interactor's Input

        public float grabStrength => m_inputDataSource.grabStrength;

        public bool grabbed => m_inputDataSource.grabbed;

        public bool onGrab => m_inputDataSource.onGrab;

        public bool onFree => m_inputDataSource.onFree;

        void Update()
        {
            m_prevHandRotation = m_handRotation;
            m_handRotation = m_inputDataSource.rootPose.rotation;
        }
    }
}
