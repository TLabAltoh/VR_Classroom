using UnityEngine;
using TLab.XR.VFX;

namespace TLab.VRClassroom
{
    public class PopupSelectable : OutlineSelectable
    {
        [SerializeField] private PopupTextManager m_popupManager;
        [SerializeField] private int m_index;

        public PopupTextManager popupManager { get => m_popupManager; set => m_popupManager = value; }

        public int index { set => m_index = value; }

        protected override void Start()
        {
            base.Start();
        }

        protected override void Update()
        {
            if (m_selected && !m_prevSelected)
            {
                TextController instance = m_popupManager.GetTextController(m_index);
                if (instance)
                {
                    instance.FadeIn();
                }
            }

            if (!m_selected && m_prevSelected)
            {
                TextController instance = m_popupManager.GetTextController(m_index);
                if (instance)
                {
                    instance.FadeOut();
                }
            }

            base.Update();
        }
    }
}
