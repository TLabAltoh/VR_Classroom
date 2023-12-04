using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TLab.XR.Interact
{
    public class Interactable : MonoBehaviour
    {
        #region REGISTRY

        private static List<Interactable> m_registry = new List<Interactable>();

        public static List<Interactable> registry => m_registry;

        protected static string REGISTRY = "[registry] ";

        protected static void Register(Interactable interactable)
        {
            if (!m_registry.Contains(interactable))
            {
                m_registry.Add(interactable);

                Debug.Log(REGISTRY + "interactable registered in the registry: " + interactable.gameObject.name);
            }
        }

        protected static void UnRegister(Interactable interactable)
        {
            if (m_registry.Contains(interactable))
            {
                m_registry.Remove(interactable);

                Debug.Log(REGISTRY + "deregistered interactable from the registry.: " + interactable.gameObject.name);
            }
        }

        #endregion

        [Header("Raycat target")]
        [SerializeField] protected bool m_colliderEnable = false;
        [SerializeField] protected Collider m_collider;

        [Header("Chain interactables")]
        [SerializeField] protected List<Interactable> m_interactableChain;

        protected List<TLabXRHand> m_hoverHands = new List<TLabXRHand>();
        protected List<TLabXRHand> m_selectHands = new List<TLabXRHand>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public Collider srufaceCollider => m_collider;

        public virtual void Hovered(TLabXRHand hand)
        {
            m_hoverHands.Add(hand);

            if (m_interactableChain != null)
            {
                m_interactableChain.ForEach((s) => s.Hovered(hand));
            }
        }

        public virtual void WhileHovered(TLabXRHand hand)
        {
            if (m_interactableChain != null)
            {
                m_interactableChain.ForEach((s) => s.WhileHovered(hand));
            }
        }

        public virtual void UnHovered(TLabXRHand hand)
        {
            m_hoverHands.Remove(hand);

            if (m_interactableChain != null)
            {
                m_interactableChain.ForEach((s) => s.UnHovered(hand));
            }
        }

        public virtual void Selected(TLabXRHand hand)
        {
            if (m_interactableChain != null)
            {
                m_interactableChain.ForEach((s) => s.Selected(hand));
            }
        }

        public virtual void WhileSelected(TLabXRHand hand)
        {
            if (m_interactableChain != null)
            {
                m_interactableChain.ForEach((s) => s.WhileSelected(hand));
            }
        }

        public virtual void UnSelected(TLabXRHand hand)
        {
            if (m_interactableChain != null)
            {
                m_interactableChain.ForEach((s) => s.UnSelected(hand));
            }
        }

        public virtual bool Raycast(Ray ray, out RaycastHit hit, float maxDistance)
        {
            if (m_collider == null || !m_collider.enabled ||  !m_colliderEnable)
            {
                hit = new RaycastHit();
                return false;
            }

            return m_collider.Raycast(ray, out hit, maxDistance);
        }

        public virtual bool Spherecast(Vector3 point, out RaycastHit hit, float maxDistance)
        {
            if (m_collider == null || !m_collider.enabled || !m_colliderEnable)
            {
                hit = new RaycastHit();
                return false;
            }

            var closestPoint = m_collider.ClosestPoint(point);
            hit = new RaycastHit();

            hit.distance = (point - closestPoint).magnitude;
            hit.point = closestPoint;

            return hit.distance < maxDistance;
        }

        protected virtual void OnEnable()
        {
            Interactable.Register(this);
        }

        protected virtual void OnDisable()
        {
            Interactable.UnRegister(this);
        }

        protected virtual void Start()
        {

        }

        protected virtual void Update()
        {

        }

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            if (m_collider == null)
            {
                m_collider = GetComponent<Collider>();

                if (m_collider != null)
                {
                    EditorUtility.SetDirty(this);
                }
            }
        }
#endif
    }
}
