using UnityEngine;

namespace TLab.XR.VRGrabber
{
    public class TLabSyncRotatable : TLabVRRotatable
    {
        private TLabSyncGrabbable m_syncGrabbable;

        protected override bool IsGrabbled
        {
            get
            {
                return m_syncGrabbable.Grabbed || m_syncGrabbable.GrabbedIndex != -1;
            }
        }

        private bool IsSyncFromOutside
        {
            get
            {
                return m_syncGrabbable.IsSyncFromOutside;
            }
        }

        public override void SetHandAngulerVelocity(Vector3 axis, float angle)
        {
            if (!IsGrabbled)
            {
                m_axis = axis;
                m_angle = angle;

                m_onShot = true;
            }
        }

        protected override void Start()
        {
            m_syncGrabbable = GetComponent<TLabSyncGrabbable>();
            if (m_syncGrabbable == null)
            {
                Destroy(this);
            }
        }

        protected override void Update()
        {
            if (!IsGrabbled && (!IsSyncFromOutside || m_onShot) && m_angle > 0.0f)
            {
                this.transform.rotation = Quaternion.AngleAxis(m_angle, m_axis) * this.transform.rotation;
                m_angle = Mathf.Clamp(m_angle - 0.1f * Time.deltaTime, 0.0f, float.MaxValue);

                m_syncGrabbable.SyncRTCTransform();
            }
            else
            {
                m_angle = 0f;
            }

            m_onShot = false;
        }
    }
}
