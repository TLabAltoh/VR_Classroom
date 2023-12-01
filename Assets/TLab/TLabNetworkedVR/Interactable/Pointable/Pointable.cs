using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.Interact
{
    public class Pointable : Interactable
    {
        private static List<Pointable> m_registry = new List<Pointable>();

        public static new List<Pointable> registry => m_registry;

        public static void Register(Pointable pointable)
        {
            if (!m_registry.Contains(pointable))
            {
                m_registry.Add(pointable);
            }
        }

        public static void UnRegister(Pointable pointable)
        {
            if (m_registry.Contains(pointable))
            {
                m_registry.Remove(pointable);
            }
        }

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
    }
}