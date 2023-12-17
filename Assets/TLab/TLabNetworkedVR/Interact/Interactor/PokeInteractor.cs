using UnityEngine;

namespace TLab.XR.Interact
{
    class PokeInteractor : Interactor
    {
        [Header("Poke Settings")]
        [SerializeField] private float m_hoverThreshold = 0.05f;
        [SerializeField] private float m_selectThreshold = 0.01f;

        [Header("Target Gesture")]
        [SerializeField] private string m_gesture;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected override void UpdateRaycast()
        {
            var minDist = float.MaxValue;
            var candidate = null as Pointable;

            Pointable.registry.ForEach((h) =>
            {
                if (h.Spherecast(m_pointer.position, out m_raycastHit, m_hoverThreshold))
                {
                    var tmp = m_raycastHit.distance;
                    if (minDist > tmp)
                    {
                        candidate = h;
                        minDist = tmp;
                    }
                }
            });

            if (candidate != null as Pointable)
            {
                var target = candidate.srufaceCollider.gameObject;

                m_raycastResult = target;

                m_interactable = candidate;

                m_interactable.Hovered(this);
            }
            else
            {
                m_interactable = null;
                m_raycastResult = null;
            }
        }

        protected override void UpdateInput()
        {
            base.UpdateInput();

            // pressed, onPress, onReleaseはポインターとパネルの距離で決まるので，
            // ここでは定義できない ...

            m_pressed = false;

            m_onPress = false;

            m_onRelease = false;

            m_angulerVelocity = m_hand.angulerVelocity;
        }

        protected override void Update()
        {
            base.Update();

            if (m_hand.currentGesture == m_gesture)
            {
                if (m_interactable != null)
                {
                    if (m_interactable.Spherecast(m_pointer.position, out m_raycastHit, m_hoverThreshold))
                    {
                        m_interactable.WhileHovered(this);

                        var distance = m_raycastHit.distance;
                        var pressed = distance < m_selectThreshold;

                        if (m_interactable.IsSelectes(this))
                        {
                            if (pressed)
                                m_interactable.WhileSelected(this);
                            else
                                m_interactable.UnSelected(this);
                        }
                        else
                        {
                            if (pressed)
                                m_interactable.Selected(this);
                        }
                    }
                    else
                    {
                        if (m_interactable.IsSelectes(this))
                            m_interactable.UnSelected(this);

                        m_interactable.UnHovered(this);
                        m_interactable = null;
                    }
                }
                else
                {
                    UpdateRaycast();
                }
            }
            else
            {
                if (m_interactable != null)
                {
                    if (m_interactable.IsSelectes(this))
                        m_interactable.UnSelected(this);

                    m_interactable.UnHovered(this);
                    m_interactable = null;
                }
            }
        }
    }
}
