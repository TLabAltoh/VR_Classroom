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
        protected Pose m_pointerPose;

        protected Pose m_pointerLocalPose;

        protected Pose m_rootPose;

        protected Pose m_rootLocalPose;

        protected float m_axis = 0.0f;

        protected bool m_pressed = false;

        protected bool m_onPress = false;

        protected bool m_onRelease = false;

        public Pose pointerPose => m_pointerPose;

        public Pose pointerLocalPose => pointerLocalPose;

        public Pose rootPose => rootPose;

        public Pose rootLocalPose => m_rootLocalPose;

        public float axis => m_axis;

        public bool pressed => m_pressed;

        public bool onPress => onPress;

        public bool onRelease => onRelease;
    }
}
