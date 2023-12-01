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

        private TLabXRHand m_mainHand;
        private TLabXRHand m_subHand;

        private Transform m_targetTransform;
        private Rigidbody m_targetRigidbody;

        private Quaternion m_mainQuaternionStart;
        private Quaternion m_thisQuaternionStart;

        public bool enabled { get => m_enabled; set => m_enabled = value; }

        public void OnMainHandGrabbed(TLabXRHand hand)
        {
            m_mainHand = hand;

            m_mainQuaternionStart = m_mainHand.grabbPoint.rotation;
            m_thisQuaternionStart = m_targetTransform.rotation;
        }

        public void OnSubHandGrabbed(TLabXRHand hand)
        {
            m_subHand = hand;
        }

        public void OnMainHandReleased(TLabXRHand hand)
        {
            if (m_mainHand == hand)
            {
                m_mainHand = null;
            }
        }

        public void OnSubHandReleased(TLabXRHand hand)
        {
            if (m_subHand == hand)
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
                    Quaternion deltaQuaternion = Quaternion.identity * m_mainHand.grabbPoint.rotation * Quaternion.Inverse(m_mainQuaternionStart);
                    m_targetRigidbody.MoveRotation(deltaQuaternion * m_thisQuaternionStart);
                }
                else
                {
                    // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                    Quaternion deltaQuaternion = Quaternion.identity * m_mainHand.grabbPoint.rotation * Quaternion.Inverse(m_mainQuaternionStart);
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
