#define HANDLE
#define POINTABLE

using System.Collections.Generic;
using UnityEngine;
using TLab.XR.Input;
using TLab.XR.Interact;

namespace TLab.XR
{
    public class TLabXRHand : MonoBehaviour
    {
        [SerializeField] private InputDataSource m_inputDataSource;

        [SerializeField] private float m_maxGrabberDistance = 0.05f;
        [SerializeField] private float m_maxPointerDistance = 0.05f;

        private RaycastHit m_pointerRaycastHit;
        private RaycastHit m_grabberRaycastHit;
        private GameObject m_pointerResult = null;
        private GameObject m_grabberResult = null;

        private Vector3 m_prevPointerVec;
        private Vector3 m_pointerVec;

        private List<Interactable> m_selectedInteractables = new List<Interactable>();

        private List<Interactable> m_hoverdInteractables = new List<Interactable>();

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public GameObject pointerResult => m_pointerResult;

        public GameObject grabberResult => m_grabberResult;

        public Transform pointer => m_inputDataSource.pointer.transform;

        public Transform grabbPointer => m_inputDataSource.grabbPointer.transform;

        public InputDataSource inputDataSource => m_inputDataSource;

        public Vector3 angulerVelocity
        {
            get
            {
                Vector3 diff = m_pointerVec - m_prevPointerVec;
                return Vector3.Cross(diff.normalized, m_pointerVec.normalized) * diff.magnitude;
            }
        }

        void Start()
        {

        }

        void Update()
        {
            m_prevPointerVec = m_pointerVec;

            m_pointerVec = m_inputDataSource.pointerPos - m_inputDataSource.pointerOrigin;

            var press = m_inputDataSource.pressed;
            var grip = m_inputDataSource.grabbed;

            var onPress = m_inputDataSource.onPress;
            var onGrab = m_inputDataSource.onGrab;

            var onRelease = m_inputDataSource.onRelease;
            var onFree = m_inputDataSource.onFree;

            if (onFree || onRelease)
            {
                m_selectedInteractables.ForEach((i) =>
                {
                    i.UnSelected(this);
                });

                m_selectedInteractables.Clear();
            }

            // Handle

            var handleMinDist = float.MaxValue;
            var handleCandidate = null as Handle;

            Handle.registry.ForEach((h) =>
            {
                if (h.Spherecast(m_inputDataSource.grabbPointerPos, out m_grabberRaycastHit, m_maxGrabberDistance))
                {
                    var tmp = m_grabberRaycastHit.distance;
                    if (handleMinDist > tmp)
                    {
                        handleCandidate = h;
                        handleMinDist = tmp;
                    }

                    Debug.Log("handle hit");
                }
                else if (m_hoverdInteractables.Contains(h))
                {
                    h.UnHovered(this);
                    m_hoverdInteractables.Remove(h);
                }
            });

#if HANDLE
            if (handleCandidate != null as Handle)
            {
                var target = handleCandidate.srufaceCollider.gameObject;

                m_grabberResult = target;

                var selectedContain = m_selectedInteractables.Contains(handleCandidate);

                var hoveredContain = m_hoverdInteractables.Contains(handleCandidate);

                // Hover

                if (!hoveredContain)
                {
                    m_hoverdInteractables.Add(handleCandidate);
                    handleCandidate.Hovered(this);
                }
                else
                {
                    handleCandidate.WhileHovered(this);
                }

                // Select

                if (onGrab && !selectedContain)
                {
                    m_selectedInteractables.Add(handleCandidate);
                    handleCandidate.Selected(this);
                }
                else if (grip && selectedContain)
                {
                    handleCandidate.WhileSelected(this);
                }
            }
            else
            {
                m_grabberResult = null;
            }

#endif

            // Pointable

            var pointerMinDist = float.MaxValue;
            var pointerCandidate = null as Pointable;

            Pointable.registry.ForEach((p) =>
            {
                if (p.Spherecast(m_inputDataSource.pointerPos, out m_pointerRaycastHit, m_maxPointerDistance))
                {
                    var tmp = m_pointerRaycastHit.distance;
                    if (pointerMinDist > tmp)
                    {
                        pointerCandidate = p;
                        pointerMinDist = tmp;
                    }

                    Debug.Log("pointable hit");
                }
                else if (m_hoverdInteractables.Contains(p))
                {
                    p.UnHovered(this);
                    m_hoverdInteractables.Remove(p);
                }
            });

#if POINTABLE
            if (pointerCandidate != null as Pointable)
            {
                var target = pointerCandidate.srufaceCollider.gameObject;

                m_pointerResult = target;

                var selectedContain = m_selectedInteractables.Contains(pointerCandidate);

                var hoveredContain = m_hoverdInteractables.Contains(pointerCandidate);

                // Hover

                if (!hoveredContain)
                {
                    m_hoverdInteractables.Add(pointerCandidate);
                    pointerCandidate.Hovered(this);
                }
                else
                {
                    pointerCandidate.WhileHovered(this);
                }

                // Select

                if (onPress && !selectedContain)
                {
                    m_selectedInteractables.Add(pointerCandidate);
                    pointerCandidate.Selected(this);
                }
                else if (press && selectedContain)
                {
                    pointerCandidate.WhileSelected(this);
                }
            }
            else
            {
                m_pointerResult = null;
            }
#endif
        }
    }
}
