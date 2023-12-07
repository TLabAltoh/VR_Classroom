using UnityEngine;

namespace TLab.XR.Interact
{
    [System.Serializable]
    public class RotationLogic
    {
        [SerializeField] private bool m_enabled = true;

        //[SerializeField] private bool m_trackX = true;
        //[SerializeField] private bool m_trackY = true;
        //[SerializeField] private bool m_trackZ = true;

        private Interactor m_mainHand;
        private Interactor m_subHand;

        private Transform m_targetTransform;
        private Rigidbody m_targetRigidbody;

        private Quaternion m_mainQuaternionStart;
        private Quaternion m_thisQuaternionStart;

        public bool enabled { get => m_enabled; set => m_enabled = value; }

        public void OnMainHandGrabbed(Interactor interactor)
        {
            m_mainHand = interactor;

            m_mainQuaternionStart = m_mainHand.pointer.rotation;
            m_thisQuaternionStart = m_targetTransform.rotation;
        }

        public void OnSubHandGrabbed(Interactor interactor)
        {
            m_subHand = interactor;
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

        public void UpdateOneHandLogic()
        {
            if (m_enabled && m_mainHand != null)
            {
                if (m_targetRigidbody != null)
                {
                    // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                    Quaternion deltaQuaternion = Quaternion.identity * m_mainHand.pointer.rotation * Quaternion.Inverse(m_mainQuaternionStart);
                    m_targetRigidbody.MoveRotation(deltaQuaternion * m_thisQuaternionStart);
                }
                else
                {
                    // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                    Quaternion deltaQuaternion = Quaternion.identity * m_mainHand.pointer.rotation * Quaternion.Inverse(m_mainQuaternionStart);
                    m_targetTransform.rotation = deltaQuaternion * m_thisQuaternionStart;
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
