using UnityEngine;
using TMPro;

namespace TLab.XR.Interact
{
    public class InteractableDebug : Interactable
    {
        [SerializeField] private TextMeshProUGUI m_hoverStateDebug;

        [SerializeField] private TextMeshProUGUI m_selectStateDebug;

        public override void Hovered(TLabXRHand hand)
        {
            base.Hovered(hand);

            m_hoverStateDebug.text = "Hovered";
        }

        public override void Selected(TLabXRHand hand)
        {
            base.Selected(hand);

            m_selectStateDebug.text = "Selected";
        }

        public override void UnHovered(TLabXRHand hand)
        {
            base.UnHovered(hand);

            m_hoverStateDebug.text = "Un Hovered";
        }

        public override void UnSelected(TLabXRHand hand)
        {
            base.UnSelected(hand);

            m_selectStateDebug.text = "Un Selected";
        }

        public override void WhileHovered(TLabXRHand hand)
        {
            base.WhileHovered(hand);

            m_hoverStateDebug.text = "While Hovered";
        }

        public override void WhileSelected(TLabXRHand hand)
        {
            base.WhileSelected(hand);

            m_selectStateDebug.text = "While Selected";
        }
    }
}
