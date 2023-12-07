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

        public override void Hovered(Interactor interactor)
        {
            base.Hovered(interactor);
        }

        public override void WhileHovered(Interactor interactor)
        {
            base.WhileHovered(interactor);
        }

        public override void UnHovered(Interactor interactor)
        {
            base.UnHovered(interactor);
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
