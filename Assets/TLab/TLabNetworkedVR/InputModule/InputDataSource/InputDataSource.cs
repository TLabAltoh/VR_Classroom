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
        protected Vector3 m_pointerOrigin;

        protected Vector3 m_pointerEnd;

        protected Pose m_pointerPose;

        protected Pose m_rootPose;

        protected float m_axis = 0.0f;

        protected bool m_pressed = false;

        protected bool m_onPress = false;

        protected bool m_onRelease = false;

        public Vector3 pointerOrigin => m_pointerOrigin;

        public Vector3 pointerEnd => m_pointerEnd;

        public Pose pointerPose => m_pointerPose;

        public Pose rootPose => m_rootPose;

        public float axis => m_axis;

        public bool pressed => m_pressed;

        public bool onPress => m_onPress;

        public bool onRelease => m_onRelease;
    }
}
