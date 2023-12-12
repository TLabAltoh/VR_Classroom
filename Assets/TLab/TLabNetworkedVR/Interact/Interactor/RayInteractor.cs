using UnityEngine;

namespace TLab.XR.Interact
{
    public class RayInteractor : Interactor
    {
        [Header("Raycast Settings")]
        [SerializeField] private float m_maxDistance = 5.0f;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        protected override void UpdateRaycast()
        {
            base.UpdateRaycast();

            var minDist = float.MaxValue;
            var candidate = null as Pointable;

            var ray = new Ray(m_hand.pointerPose.position, m_hand.pointerPose.forward);

            Pointable.registry.ForEach((h) =>
            {
                if (h.Raycast(ray, out m_raycastHit, m_maxDistance))
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
                m_pointer.position = m_raycastHit.point;

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

            m_pressed = m_hand.pressed;

            m_onPress = m_hand.onPress;

            m_onRelease = m_hand.onRelease;

            m_angulerVelocity = m_hand.angulerVelocity;
        }

        protected override void Process()
        {
            base.Process();

            if (m_interactable != null)
            {
                var ray = new Ray(m_hand.pointerPose.position, m_hand.pointerPose.forward);
                if (m_interactable.Raycast(ray, out m_raycastHit, m_maxDistance))
                {
                    m_pointer.position = m_raycastHit.point;

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
