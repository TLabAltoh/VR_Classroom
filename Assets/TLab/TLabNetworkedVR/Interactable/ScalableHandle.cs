using UnityEngine;

namespace TLab.XR.Interact
{
    public class ScalableHandle : Interactable
    {
        private TLabXRHand m_hand;

        private ScaleLogic m_logics;

        public override void Selected(TLabXRHand hand)
        {
            if (m_hand == null)
            {
                m_hand = hand;

                m_logics.HandleGrabbed(this);
            }

            base.Selected(hand);
        }

        public override void Unselected(TLabXRHand hand)
        {
            if (m_hand == hand)
            {
                m_hand = null;

                m_logics.HandleUnGrabbed(this);
            }

            base.Unselected(hand);
        }

        public Vector3 GetHandPos()
        {
            return m_hand.grabbPoint.position;
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
