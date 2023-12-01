using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace TLab.XR.Interact
{
    public class Interactable : MonoBehaviour
    {
        private static List<Interactable> m_registry = new List<Interactable>();

        public static List<Interactable> registry => m_registry;

        // TODO: Added hover handling ...

        public static void Register(Interactable selectable)
        {
            if (!m_registry.Contains(selectable))
            {
                m_registry.Add(selectable);
            }
        }

        public static void UnRegister(Interactable selectable)
        {
            if (m_registry.Contains(selectable))
            {
                m_registry.Remove(selectable);
            }
        }

        [Header("Raycat target")]
        [SerializeField] protected bool m_colliderEnable = false;
        [SerializeField] protected Collider m_collider;

        [Header("Chain interactables")]
        [SerializeField] protected List<Interactable> m_interactableChain;

        protected List<TLabXRHand> m_hoverHands;
        protected List<TLabXRHand> m_selectHands;

        public Collider srufaceCollider => m_collider;

        public virtual void Hovered(TLabXRHand hand)
        {
            Debug.Log("hovered. name: " + gameObject.name);

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
            Debug.Log("unhovered. name: " + gameObject.name);

            m_hoverHands.Remove(hand);

            if (m_interactableChain != null)
            {
                m_interactableChain.ForEach((s) => s.UnHovered(hand));
            }
        }

        public virtual void Selected(TLabXRHand hand)
        {
            Debug.Log("selected. name: " + gameObject.name);

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
            Debug.Log("unselected. name: " + gameObject.name);

            if (m_interactableChain != null)
            {
                m_interactableChain.ForEach((s) => s.UnSelected(hand));
            }
        }

        public virtual bool Raycast(Ray ray, out RaycastHit hit, float maxDistance)
        {
            if (m_collider == null || !m_colliderEnable)
            {
                hit = new RaycastHit();
                return false;
            }

            return m_collider.Raycast(ray, out hit, maxDistance);
        }

        public virtual bool Spherecast(Vector3 point, out RaycastHit hit, float maxDistance)
        {
            if (m_collider == null || !m_colliderEnable)
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
            m_collider = GetComponent<Collider>();

            if(m_collider != null)
            {
                EditorUtility.SetDirty(this);
            }
        }
#endif
    }
}
