using UnityEngine;

namespace TLab.XR.Interact
{
    [System.Serializable]
    public class PositionLogic
    {
        [SerializeField] private bool m_enabled = true;

        private Interactor m_mainHand;
        private Interactor m_subHand;

        private Transform m_targetTransform;
        private Rigidbody m_targetRigidbody;

        private Vector3 m_mainPositionOffset;
        private Vector3 m_subPositionOffset;

        public bool enabled
        {
            get => m_enabled;
            set
            {
                m_enabled = value;
            }
        }

        public void OnMainHandGrabbed(Interactor interactor)
        {
            m_mainHand = interactor;

            m_mainPositionOffset = m_mainHand.pointer.InverseTransformPoint(m_targetTransform.position);
        }

        public void OnSubHandGrabbed(Interactor interactor)
        {
            m_subHand = interactor;

            m_subPositionOffset = m_subHand.pointer.InverseTransformPoint(m_targetTransform.position);
        }

        public void OnMainHandReleased(Interactor interactor)
        {
            if (m_mainHand == interactor)
            {
                m_mainHand = null;
            }
        }

        public void OnSubHandReleased(Interactor interactor)
        {
            if (m_subHand == interactor)
            {
                m_subHand = null;
            }
        }

        public void UpdateTwoHandLogic()
        {
            if (m_enabled && m_mainHand != null && m_subHand != null)
            {
                var updatedPositionMain = m_mainHand.pointer.TransformPoint(m_mainPositionOffset);
                var updatedPositionSub = m_subHand.pointer.TransformPoint(m_subPositionOffset);
                var updatedPosition = Vector3.Lerp(updatedPositionMain, updatedPositionSub, 0.5f);

                if (m_targetRigidbody)
                {
                    m_targetRigidbody.MovePosition(updatedPosition);
                }
                else
                {
                    m_targetTransform.position = updatedPosition;
                }
            }
        }

        public void UpdateOneHandLogic()
        {
            if (m_enabled && m_mainHand != null)
            {
                var updatedPosition = m_mainHand.pointer.TransformPoint(m_mainPositionOffset);
                if (m_targetRigidbody)
                {
                    m_targetRigidbody.MovePosition(updatedPosition);
                }
                else
                {
                    m_targetTransform.position = updatedPosition;
                }
            }
        }

        public void Start(Transform targetTransform, Rigidbody targetRigidbody = null)
        {
            m_targetTransform = targetTransform;
            m_targetRigidbody = targetRigidbody;

            enabled = m_enabled;
        }
    }
}
