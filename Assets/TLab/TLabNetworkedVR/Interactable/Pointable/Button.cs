using UnityEngine;
using UnityEngine.Events;

namespace TLab.XR.Interact
{
    public class Button : Pointable
    {
        [SerializeField] private UnityEvent[] m_onPress;

        [SerializeField] private UnityEvent[] m_onRelease;

        public override void Hovered(TLabXRHand hand)
        {
            base.Hovered(hand);
        }

        public override void Selected(TLabXRHand hand)
        {
            base.Selected(hand);

            foreach (var callback in m_onPress)
            {
                callback.Invoke();
            }
        }

        public override void UnHovered(TLabXRHand hand)
        {
            base.UnHovered(hand);
        }

        public override void UnSelected(TLabXRHand hand)
        {
            base.UnSelected(hand);

            foreach (var callback in m_onRelease)
            {
                callback.Invoke();
            }
        }

        public override void WhileHovered(TLabXRHand hand)
        {
            base.WhileHovered(hand);
        }

        public override void WhileSelected(TLabXRHand hand)
        {
            base.WhileSelected(hand);
        }
    }
}
