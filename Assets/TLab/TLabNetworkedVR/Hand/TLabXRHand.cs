#define GRABBABLE
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
        [SerializeField] private Transform m_pointerPos;
        [SerializeField] private Transform m_grabbPoint;
        [SerializeField] private float m_maxGrabberDistance = 0.1f;
        [SerializeField] private float m_maxPointerDistance = 0.1f;

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

        public Transform pointerPos => m_pointerPos;

        public Transform grabbPoint => m_grabbPoint;

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

            m_pointerVec = m_inputDataSource.pointerEnd;

            var grip = m_inputDataSource.pressed;
            var onPress = m_inputDataSource.onPress;
            var onRelease = m_inputDataSource.onRelease;

            if (onRelease)
            {
                m_selectedInteractables.ForEach((i) =>
                {
                    i.UnSelected(this);
                });

                m_selectedInteractables.Clear();
            }

            // Grabbable

            var grabberMinDist = float.MaxValue;
            var grabbCandidate = null as Grabbable;

            foreach(var c in Grabbable.registry.Values)
            {
                var g = c as Grabbable;

                // Žè‚Æ’Í‚ñ‚Å‚¢‚éGrabbable‚ÌŠÔ‚ÉGrabbable‚ªŠ„‚èž‚ÝCGrabbable‚ªØ‚è‘Ö‚í‚é‚Ì‚ð‚±‚±‚Å–h‚® ...
                if(g.mainHand == this || g.subHand == this)
                {
                    grabbCandidate = g;
                    break;
                }

                if (g.Spherecast(m_grabbPoint.position, out m_grabberRaycastHit, m_maxGrabberDistance))
                {
                    var tmp = m_grabberRaycastHit.distance;
                    if (grabberMinDist > tmp)
                    {
                        grabbCandidate = g;
                        grabberMinDist = tmp;
                    }

                    Debug.Log("grabbable hit");
                }
            }

#if GRABBABLE
            if (grabbCandidate != null as Grabbable)
            {
                var target = grabbCandidate.srufaceCollider.gameObject;

                m_grabberResult = target;

                var selectedContain = m_selectedInteractables.Contains(grabbCandidate);

                var hoveredContain = m_hoverdInteractables.Contains(grabbCandidate);

                // Hover

                if (!hoveredContain)
                {
                    m_hoverdInteractables.Add(grabbCandidate);
                    grabbCandidate.Hovered(this);
                }
                else
                {
                    grabbCandidate.WhileHovered(this);
                }

                // Select

                if (onPress && !selectedContain)
                {
                    m_selectedInteractables.Add(grabbCandidate);
                    grabbCandidate.Selected(this);
                }
                else if (grip && selectedContain)
                {
                    grabbCandidate.WhileSelected(this);
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
                if (p.Spherecast(m_pointerPos.position, out m_pointerRaycastHit, m_maxPointerDistance))
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
                var target = grabbCandidate.srufaceCollider.gameObject;

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
                else if (onRelease && selectedContain)
                {
                    pointerCandidate.UnSelected(this);
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
