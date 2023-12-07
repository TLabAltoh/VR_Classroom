using UnityEngine;

namespace TLab.XR.Input
{
    public struct Pose
    {
        public Vector3 position;
        public Quaternion rotation;
    }

    public class InputDataSource : MonoBehaviour
    {
        [Header("Ray Interactor's Pointer")]
        [SerializeField] protected GameObject m_pointer;

        [Header("Grabb Interactor's Pointer")]
        [SerializeField] protected GameObject m_grabbPointer;


        protected Vector3 m_pointerOrigin;

        protected Vector3 m_pointerPos;

        protected Vector3 m_grabbPointerPos;

        protected Input.Pose m_pointerPose;

        protected Input.Pose m_rootPose;

        protected float m_pressStrength = 0.0f;

        protected float m_grabStrength = 0.0f;


        protected bool m_grabbed = false;

        protected bool m_onGrab = false;

        protected bool m_onFree = false;


        protected bool m_pressed = false;

        protected bool m_onPress = false;

        protected bool m_onRelease = false;

        protected string m_currentGesture = "";


        public GameObject pointer => m_pointer;

        public GameObject grabbPointer => m_grabbPointer;


        public Vector3 pointerOrigin => m_pointerOrigin;

        public Vector3 pointerPos => m_pointerPos;


        public Vector3 grabbPointerPos => m_grabbPointerPos;


        public Pose pointerPose => m_pointerPose;

        public Pose rootPose => m_rootPose;


        public float pressStrength => m_pressStrength;

        public float grabStrength => m_grabStrength;


        public string currentGesture => m_currentGesture;


        public bool grabbed => m_grabbed;

        public bool onGrab => m_onGrab;

        public bool onFree => m_onFree;


        public bool pressed => m_pressed;

        public bool onPress => m_onPress;

        public bool onRelease => m_onRelease;
    }
}
