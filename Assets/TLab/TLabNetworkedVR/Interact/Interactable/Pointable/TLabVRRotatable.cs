using UnityEngine;

namespace TLab.XR.VRGrabber
{
    public class TLabVRRotatable : MonoBehaviour
    {
        private TLabVRGrabbable m_grabbable;
        protected Vector3 m_axis;
        protected float m_angle;
        protected bool m_onShot = false;

        protected virtual bool IsGrabbled
        {
            get
            {
                return m_grabbable.Grabbed;
            }
        }

        public virtual void SetHandAngulerVelocity(Vector3 axis, float angle)
        {
            if (!IsGrabbled)
            {
                m_axis = axis;
                m_angle = angle;

                m_onShot = true;
            }
        }

        protected virtual void Start()
        {
            m_grabbable = GetComponent<TLabVRGrabbable>();
            if (m_grabbable == null)
            {
                Destroy(this);
            }
        }

        protected virtual void Update()
        {
            if (!IsGrabbled && m_onShot && m_angle > 0.0f)
            {
                this.transform.rotation = Quaternion.AngleAxis(m_angle, m_axis) * this.transform.rotation;
                m_angle = Mathf.Clamp(m_angle - 0.1f * Time.deltaTime, 0.0f, float.MaxValue);
            }
            else
            {
                m_angle = 0f;
            }

            m_onShot = false;
        }
    }
}
