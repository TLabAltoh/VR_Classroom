using UnityEngine;

namespace TLab.XR.Interact
{
    public class Rotatable : Interactable
    {
        private Grabbable m_grabbable;

        private Vector3 m_axis;

        private float m_angle;

        private bool m_onShot = false;

        private const float DURATION = 0.1f;

        private bool syncFromOutside => m_grabbable.syncFromOutside;

        private bool grabbled => m_grabbable.grabbed || m_grabbable.grabbedIndex != -1;

        public void SetHandAngulerVelocity(Vector3 axis, float angle)
        {
            if (!grabbled)
            {
                m_axis = axis;
                m_angle = angle;

                m_onShot = true;
            }
        }

        protected override void Start()
        {
            base.Start();

            m_grabbable = GetComponent<Grabbable>();
        }

        protected override void Update()
        {
            base.Update();

            if (!grabbled && (!syncFromOutside || m_onShot) && m_angle > 0.0f)
            {
                transform.rotation = Quaternion.AngleAxis(m_angle, m_axis) * transform.rotation;
                m_angle = Mathf.Clamp(m_angle - 0.1f * Time.deltaTime, 0.0f, float.MaxValue);

                if(m_grabbable != null)
                {
                    m_grabbable.SyncRTCTransform();
                }
            }
            else
            {
                m_angle = 0f;
            }

            m_onShot = false;
        }
    }
}
