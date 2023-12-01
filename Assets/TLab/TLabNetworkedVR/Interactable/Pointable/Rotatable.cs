using UnityEngine;

namespace TLab.XR.Interact
{
    public class Rotatable : Pointable
    {
        [SerializeField] private float m_rotateSpeed = 100f;

        // 名前がややこしいが，Grabbableと共存するコンポーネントである．
        // 単体では動かない．

        // TODO: 単体で動かす

        private Grabbable m_grabbable;

        private Vector3 m_axis;

        private float m_angle;

        private bool m_onShot = false;

        private const float DURATION = 0.1f;

        public static float ZERO_ANGLE = 0.0f;

        private bool syncFromOutside => m_grabbable.syncFromOutside;

        private bool grabbled => m_grabbable.grabbed || m_grabbable.grabbedIndex != -1;

        public void Stop()
        {
            if (!grabbled)
            {
                m_axis = Vector3.one;
                m_angle = ZERO_ANGLE;

                m_onShot = true;
            }
        }

        public override void Selected(TLabXRHand hand)
        {
            base.Selected(hand);
        }

        public override void UnSelected(TLabXRHand hand)
        {
            base.UnSelected(hand);
        }

        public override void WhileSelected(TLabXRHand hand)
        {
            base.WhileSelected(hand);

            if (hand.inputDataSource.pressed)
            {
                if (!grabbled)
                {
                    var angulerVel = hand.angulerVelocity;

                    m_axis = angulerVel.normalized;
                    m_angle = angulerVel.magnitude * m_rotateSpeed;

                    m_onShot = true;
                }
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

            if (!grabbled && (!syncFromOutside || m_onShot) && m_angle > ZERO_ANGLE)
            {
                transform.rotation = Quaternion.AngleAxis(m_angle, m_axis) * transform.rotation;
                m_angle = Mathf.Clamp(m_angle - DURATION * Time.deltaTime, ZERO_ANGLE, float.MaxValue);

                if(m_grabbable != null)
                {
                    m_grabbable.SyncRTCTransform();
                }
            }
            else
            {
                m_angle = ZERO_ANGLE;
            }

            m_onShot = false;
        }
    }
}
