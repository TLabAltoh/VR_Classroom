using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.Interact
{
    public class Interactable : MonoBehaviour
    {
        private static List<Interactable> m_registory = new List<Interactable>();

        public static List<Interactable> registory => m_registory;

        // TODO: Added hover handling ...

        public static void Register(Interactable selectable)
        {
            if (!m_registory.Contains(selectable))
            {
                m_registory.Add(selectable);
            }
        }

        public static void UnRegister(Interactable selectable)
        {
            if (m_registory.Contains(selectable))
            {
                m_registory.Remove(selectable);
            }
        }

        [Header("Raycat target")]
        [SerializeField] protected Collider m_collider;

        [Header("Chain selectables")]
        [SerializeField] protected List<Interactable> m_selectableChain;

        public virtual void Hovered(TLabXRHand hand)
        {
            Debug.Log("hovered");

        }

        public virtual void WhileHovered(TLabXRHand hand)
        {

        }

        public virtual void UnHovered(TLabXRHand hand)
        {

        }

        public virtual void Selected(TLabXRHand hand)
        {
            Debug.Log("selected");

            if (m_selectableChain != null)
            {
                m_selectableChain.ForEach((s) => s.Selected(hand));
            }
        }

        public virtual void WhileSelected(TLabXRHand hand)
        {
            if (m_selectableChain != null)
            {
                m_selectableChain.ForEach((s) => s.WhileSelected(hand));
            }
        }

        public virtual void UnSelected(TLabXRHand hand)
        {
            Debug.Log("unselected");

            if (m_selectableChain != null)
            {
                m_selectableChain.ForEach((s) => s.UnSelected(hand));
            }
        }

        public virtual bool Raycast(Ray ray, out RaycastHit hit, float maxDistance)
        {
            if (m_collider == null)
            {
                hit = new RaycastHit();
                return false;
            }

            return m_collider.Raycast(ray, out hit, maxDistance);
        }

        public virtual bool Spherecast(Vector3 point, out RaycastHit hit, float maxDistance)
        {
            if (m_collider == null)
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
    }
}
