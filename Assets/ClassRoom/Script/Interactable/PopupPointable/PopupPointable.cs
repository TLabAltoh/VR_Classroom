using UnityEngine;
using TLab.XR;
using TLab.XR.Interact;

namespace TLab.VRClassroom
{
    public class PopupPointable : OutlinePointable
    {
        [SerializeField] private PopupTextManager m_popupManager;
        [SerializeField] private int m_index;

        public PopupTextManager popupManager { get => m_popupManager; set => m_popupManager = value; }

        public int index { set => m_index = value; }

        protected override void Start()
        {
            base.Start();
        }

        public override void Hovered(TLabXRHand hand)
        {
            base.Hovered(hand);

            var instance = m_popupManager.GetTextController(m_index);
            if (instance)
            {
                instance.FadeIn();
            }
        }

        public override void UnHovered(TLabXRHand hand)
        {
            base.UnHovered(hand);

            var instance = m_popupManager.GetTextController(m_index);
            if (instance)
            {
                instance.FadeOut();
            }
        }
    }
}
