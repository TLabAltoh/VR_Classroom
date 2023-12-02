using System.Collections.Generic;

namespace TLab.XR.Interact
{
    public class Handle : Interactable
    {
        #region REGISTRY

        private static List<Handle> m_registry = new List<Handle>();

        public static new List<Handle> registry => m_registry;

        public static void Register(Handle handle)
        {
            if (!m_registry.Contains(handle))
            {
                m_registry.Add(handle);
            }
        }

        public static void UnRegister(Handle handle)
        {
            if (m_registry.Contains(handle))
            {
                m_registry.Remove(handle);
            }
        }

        #endregion

        protected bool m_hovered = false;

        protected bool m_selected = false;

        public bool hovered => m_hovered;

        public bool selected => m_selected;

        public override void Hovered(TLabXRHand hand)
        {
            m_hovered = true;

            base.Hovered(hand);
        }

        public override void WhileHovered(TLabXRHand hand)
        {
            base.WhileHovered(hand);
        }

        public override void UnHovered(TLabXRHand hand)
        {
            m_hovered = false;

            base.UnHovered(hand);
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Handle.Register(this);
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            Handle.UnRegister(this);
        }
    }
}
