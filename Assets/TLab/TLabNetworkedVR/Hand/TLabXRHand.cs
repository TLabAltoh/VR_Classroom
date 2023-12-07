using UnityEngine;
using TLab.XR.Input;

namespace TLab.XR
{
    public class TLabXRHand : MonoBehaviour
    {
        [SerializeField] private InputDataSource m_inputDataSource;

        private Vector3 m_prevPointerVec;
        private Vector3 m_pointerVec;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        // Input Data Source

        public InputDataSource inputDataSource => m_inputDataSource;

        // Gesture

        public string currentGesture => m_inputDataSource.currentGesture;

        // Ray Interactor's Pointer

        public bool pressed => m_inputDataSource.pressed;

        public bool onPress => m_inputDataSource.onPress;

        public bool onRelease => m_inputDataSource.onRelease;

        public float pressStrength => m_inputDataSource.pressStrength;

        public Vector3 pointerPos => m_inputDataSource.pointerPos;

        public Transform pointer => m_inputDataSource.pointer.transform;

        // Grab Interactor's Pointer

        public bool grabbed => m_inputDataSource.grabbed;

        public bool onGrab => m_inputDataSource.onGrab;

        public bool onFree => m_inputDataSource.onFree;

        public float grabStrength => m_inputDataSource.grabStrength;

        public Vector3 grabbPointerPos => m_inputDataSource.grabbPointerPos;

        public Transform grabbPointer => m_inputDataSource.grabbPointer.transform;


        public Vector3 angulerVelocity
        {
            get
            {
                Vector3 diff = m_pointerVec - m_prevPointerVec;
                return Vector3.Cross(diff.normalized, m_pointerVec.normalized) * diff.magnitude;
            }
        }

        void Update()
        {
            m_prevPointerVec = m_pointerVec;

            m_pointerVec = m_inputDataSource.pointerPos - m_inputDataSource.pointerOrigin;
        }
    }
}
