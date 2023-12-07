using UnityEngine;
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

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);

            var instance = m_popupManager.GetTextController(m_index);
            if (instance)
            {
                instance.FadeIn();
            }
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);

            var instance = m_popupManager.GetTextController(m_index);
            if (instance)
            {
                instance.FadeOut();
            }
        }
    }
}
