using UnityEngine;

namespace TLab.XR.Interact
{
    public class ScalableHandle : Handle
    {
        private TLabXRHand m_hand;

        private ScaleLogic m_logics;

        public Vector3 handPos => m_hand.grabbPointer.position;

        public override void Selected(TLabXRHand hand)
        {
            base.Selected(hand);

            if (m_hand == null)
            {
                m_hand = hand;

                m_logics.HandleGrabbed(this);
            }
        }

        public override void UnSelected(TLabXRHand hand)
        {
            base.UnSelected(hand);

            if (m_hand == hand)
            {
                m_hand = null;

                m_logics.HandleUnGrabbed(this);
            }
        }

        public override void WhileSelected(TLabXRHand hand)
        {
            base.WhileSelected(hand);
        }

        public void RegistScalable(ScaleLogic logic)
        {
            if (m_logics == null)
            {
                m_logics = logic;
            }
        }

        public void UnRegistScalable(ScaleLogic logic)
        {
            if (m_logics == logic)
            {
                m_logics = null;
            }
        }
    }
}
