using UnityEngine;

namespace TLab.XR.Input
{
    public class InputDataSource : MonoBehaviour
    {
        // Pointer Pose

        protected Pose m_pointerPose;
        protected Pose m_rootPose;

        public Pose pointerPose => m_pointerPose;
        public Pose rootPose => m_rootPose;

        // Gesture

        protected string m_currentGesture = "";

        public string currentGesture => m_currentGesture;

        // Grab Interactor

        protected float m_grabStrength = 0.0f;
        protected bool m_grabbed = false;
        protected bool m_onGrab = false;
        protected bool m_onFree = false;

        public float grabStrength => m_grabStrength;

        public bool grabbed => m_grabbed;

        public bool onGrab => m_onGrab;

        public bool onFree => m_onFree;

        // Ray Interactor

        protected float m_pressStrength = 0.0f;
        protected bool m_pressed = false;
        protected bool m_onPress = false;
        protected bool m_onRelease = false;

        public float pressStrength => m_pressStrength;

        public bool pressed => m_pressed;

        public bool onPress => m_onPress;

        public bool onRelease => m_onRelease;
    }
}
