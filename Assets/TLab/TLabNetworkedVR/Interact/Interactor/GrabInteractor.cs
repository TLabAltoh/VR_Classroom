using UnityEngine;

namespace TLab.XR.Interact
{
    public class GrabInteractor : Interactor
    {
        [Header("Grab Settings")]
        [SerializeField] private float m_hoverThreshold = 0.05f;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected override void UpdateRaycast()
        {
            var minDist = float.MaxValue;
            var candidate = null as Handle;

            Handle.registry.ForEach((h) =>
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

            if (candidate != null as Handle)
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

            m_pressed = m_hand.grabbed;

            m_onPress = m_hand.onGrab;

            m_onRelease = m_hand.onFree;

            m_angulerVelocity = m_hand.angulerVelocity;
        }

        protected override void Process()
        {
            base.Process();

            if (m_interactable != null)
            {
                if (m_pressed || m_interactable.Spherecast(m_pointer.position, out m_raycastHit, m_hoverThreshold))
                {
                    m_interactable.WhileHovered(this);

                    if (m_interactable.IsSelectes(this))
                    {
                        if (m_pressed)
                        {
                            m_interactable.WhileSelected(this);
                        }
                        else
                        {
                            m_interactable.UnSelected(this);
                        }
                    }
                    else
                    {
                        if (m_onPress)
                        {
                            m_interactable.Selected(this);
                        }
                    }
                }
                else
                {
                    if (m_interactable.IsSelectes(this))
                    {
                        m_interactable.UnSelected(this);
                    }

                    m_interactable.UnHovered(this);
                    m_interactable = null;
                }
            }
            else
            {
                UpdateRaycast();
            }
        }
    }
}
