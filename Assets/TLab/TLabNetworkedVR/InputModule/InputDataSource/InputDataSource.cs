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
        [Header("Pointer")]

        [SerializeField] protected GameObject m_pointer;

        [SerializeField] protected GameObject m_grabbPointer;

        protected Vector3 m_pointerOrigin;

        protected Vector3 m_pointerPos;

        protected Vector3 m_grabbPointerPos;

        protected Pose m_pointerPose;

        protected Pose m_rootPose;


        protected float m_triggerStrength = 0.0f;

        protected float m_grabStrength = 0.0f;


        protected bool m_grabbed = false;

        protected bool m_onGrab = false;

        protected bool m_onFree = false;


        protected bool m_pressed = false;

        protected bool m_onPress = false;

        protected bool m_onRelease = false;

        public GameObject pointer => m_pointer;

        public GameObject grabbPointer => m_grabbPointer;

        public Vector3 pointerOrigin => m_pointerOrigin;

        public Vector3 pointerPos => m_pointerPos;

        public Vector3 grabbPointerPos => m_grabbPointerPos;

        public Pose pointerPose => m_pointerPose;

        public Pose rootPose => m_rootPose;

        public float triggerStrength => m_triggerStrength;

        public float grabStrength => m_grabStrength;


        public bool grabbed => m_grabbed;

        public bool onGrab => m_onGrab;

        public bool onFree => m_onFree;


        public bool pressed => m_pressed;

        public bool onPress => m_onPress;

        public bool onRelease => m_onRelease;
    }
}
