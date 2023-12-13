using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.Interact
{
    [RequireComponent(typeof(ExclusiveController))]
    public class Rotatable : Pointable
    {
        #region REGISTRY

        private static List<Rotatable> m_registry = new List<Rotatable>();

        public static new List<Rotatable> registry => m_registry;

        public static void Register(Rotatable rotatable)
        {
            if (!m_registry.Contains(rotatable))
            {
                m_registry.Add(rotatable);
            }
        }

        public static void UnRegister(Rotatable rotatable)
        {
            if (m_registry.Contains(rotatable))
            {
                m_registry.Remove(rotatable);
            }
        }

        #endregion

        [SerializeField] private float m_rotateSpeed = 10f;

        private ExclusiveController m_controller;

        private Vector3 m_axis;

        private float m_angle;

        private bool m_onShot = false;

        private const float BIAS = 10f;

        private const float DURATION = 0.1f;

        public static float ZERO_ANGLE = 0.0f;

        public float rotateSpeed { get => m_rotateSpeed; set => m_rotateSpeed = value; }

        private bool grabbled => m_controller.grabbed;

        private bool syncFromOutside => m_controller.syncFromOutside;

        public void Stop()
        {
            if (!grabbled)
            {
                m_axis = Vector3.one;
                m_angle = ZERO_ANGLE;

                m_onShot = false;
            }
        }

        public override void Selected(Interactor interactor)
        {
            base.Selected(interactor);
        }

        public override void UnSelected(Interactor interactor)
        {
            base.UnSelected(interactor);
        }

        public override void WhileSelected(Interactor interactor)
        {
            base.WhileSelected(interactor);

            if (interactor.pressed)
            {
                if (!grabbled)
                {
                    var angulerVel = interactor.angulerVelocity;

                    m_axis = angulerVel.normalized;
                    m_angle = angulerVel.magnitude * m_rotateSpeed * BIAS;

                    m_onShot = true;
                }
            }
        }

        protected override void Start()
        {
            base.Start();

            m_controller = GetComponent<ExclusiveController>();
        }

        protected override void Update()
        {
            base.Update();

            // controller == null --> ‚¾‚ê‚à’Í‚ñ‚Å‚¢‚È‚¢‚Ì‚ÅOK
            // controller != null --> ‚¾‚ê‚à’Í‚ñ‚Å‚¢‚È‚¯‚ê‚ÎOK

            if ((m_controller == null || !grabbled) && (!syncFromOutside || m_onShot) && m_angle > ZERO_ANGLE)
            {
                transform.rotation = Quaternion.AngleAxis(m_angle, m_axis) * transform.rotation;
                m_angle = Mathf.Clamp(m_angle - DURATION * Time.deltaTime, ZERO_ANGLE, float.MaxValue);

                m_controller?.SyncRTCTransform();
            }
            else
            {
                m_angle = ZERO_ANGLE;
            }

            m_onShot = false;
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Rotatable.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Rotatable.UnRegister(this);
        }
    }
}
