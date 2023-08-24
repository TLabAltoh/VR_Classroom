using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TLab.XR.VRGrabber.VFX;

namespace TLab.XR.VRGrabber
{
    public class TLabVRTrackingHand : MonoBehaviour
    {
        [Header("Hand Settings")]

        [Tooltip("OVRHand controlled by this hand")]
        [SerializeField] private OVRHand m_hand;

        [Tooltip("This hand-controlled laser pointer")]
        [SerializeField] private LaserPointer m_laserPointer;

        [Tooltip("Maximum length of laser pointer")]
        [SerializeField] private float m_maxDistance = 10.0f;

        [Tooltip("When grabbing an object, use this object's Transform.forward as a Ray to determine the object")]
        [SerializeField] private Transform m_grabbAnchor;

        [Tooltip("Specify the layer of the object you want to grab and rotate")]
        [SerializeField] private LayerMask m_layerMask;

        [Header("Gesture")]

        [Tooltip("Bones to be controlled by hand tracking are retrieved from the skeleton")]
        [SerializeField] private OVRSkeleton m_skeleton;

        [Tooltip("Saved gesture (local Transform.position of the hand bone)")]
        [SerializeField] private List<Gesture> m_gestures;

        [Tooltip("Define how much error to allow for when judging a gesture versus a hand bone gesture")]
        [SerializeField] private float threshold = 0.05f;

        [Tooltip("While in debug mode, the gesture can be saved by pressing the space button")]
        [SerializeField] private bool m_debugMode;

        private List<OVRBone> m_fingerBones;
        private TLabVRGrabbable m_grabbable;

        private RaycastHit m_raycastHit;

        private bool m_grabbPrev = false;

        private bool m_skeltonInitialized;

        private Vector3 m_prevRotateAnchor;
        private Vector3 m_currentRotateAnchor;

        //
        private const string thisName = "[tlabvrtrackinghand] ";

        [System.Serializable]
        public struct Gesture
        {
            public string name;
            public List<Vector3> fingerDatas;
        }

        public bool SkeltonInitialized
        {
            get
            {
                return m_skeltonInitialized;
            }
        }

        public TLabVRGrabbable CurrentGrabbable
        {
            get
            {
                return m_grabbable;
            }
        }

        public Transform PointerPose
        {
            get
            {
                return m_hand.PointerPose.transform;
            }
        }

        private Vector3 HandAngulerVelocity
        {
            get
            {
                Vector3 diff = m_currentRotateAnchor - m_prevRotateAnchor;
                return Vector3.Cross(diff, m_hand.PointerPose.forward).normalized * diff.magnitude;
            }
        }

        public OVRBone GetFingerBone(OVRSkeleton.BoneId id)
        {
            if (m_skeltonInitialized == false) return null;
            return m_skeleton.Bones[(int)id];
        }

        /// <summary>
        /// Record the current position of the OVRBone relative to the hand
        /// Right-click to copy from the Inspector and paste after playback is complete
        /// </summary>
        private void SavePose()
        {
            Gesture g = new Gesture();
            g.name = "New Gesture";

            List<Vector3> data = new List<Vector3>();
            foreach (var bone in m_fingerBones)
                data.Add(m_skeleton.transform.InverseTransformPoint(bone.Transform.position));

            g.fingerDatas = data;
            m_gestures.Add(g);
        }

        /// <summary>
        /// Get the current gesture
        /// </summary>
        /// <returns></returns>
        private string DetectGesture()
        {
            string result = null;
            float currentMin = Mathf.Infinity;

            foreach (var gesture in m_gestures)
            {
                float sumDistance = 0.0f;
                bool isDiscarded = false;
                for (int i = 0; i < m_fingerBones.Count; i++)
                {
                    Vector3 currentData = m_skeleton.transform.InverseTransformPoint(m_fingerBones[i].Transform.position);
                    float distance = Vector3.Distance(currentData, gesture.fingerDatas[i]);

                    if (distance > threshold)
                    {
                        isDiscarded = true;
                        break;
                    }

                    sumDistance += distance;
                }

                if (!isDiscarded && sumDistance < currentMin)
                {
                    currentMin = sumDistance;
                    result = gesture.name;
                }
            }

            return result;
        }

        /// <summary>
        /// Returns true only on the first frame when the gesture is judged
        /// </summary>
        /// <returns></returns>
        private bool GetGrabbDown()
        {
            bool grabb = DetectGesture() == "Grabb";
            bool grabbDown = grabb;

            if (m_grabbPrev == true) grabbDown = false;

            m_grabbPrev = grabb;

            return grabbDown;
        }

        /// <summary>
        /// Task to wait until skeleton's bones are initialized
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitForSkeltonInitialized()
        {
            // https://communityforums.atmeta.com/t5/Unity-VR-Development/Bones-list-is-empty/td-p/880261
            while (m_skeleton.Bones.Count == 0) yield return null;

            m_fingerBones = new List<OVRBone>(m_skeleton.Bones);
            m_skeltonInitialized = true;
        }

        void Start()
        {
            m_skeltonInitialized = false;
            if (m_skeleton != null) StartCoroutine(WaitForSkeltonInitialized());
        }

        void Update()
        {
            if (m_debugMode && Input.GetKeyDown(KeyCode.Space))
            {
                SavePose();
                return;
            }

            if (!m_skeltonInitialized) return;

            m_prevRotateAnchor = m_currentRotateAnchor;
            m_currentRotateAnchor = m_hand.PointerPose.position + m_hand.PointerPose.forward * 1f;

            bool grip = DetectGesture() == "Grabb";
            bool grabbDown = GetGrabbDown();

            m_laserPointer.maxLength = !grip ? m_maxDistance : 0.0f;

            //if(Physics.Raycast(m_grabbAnchor.position, m_grabbAnchor.forward, out m_raycastHit, 0.25f, m_layerMask))
            if (Physics.SphereCast(m_grabbAnchor.position, 0.025f, m_grabbAnchor.forward, out m_raycastHit, 0.25f, m_layerMask))
            {
                if (m_grabbable != null)
                {
                    if (!grip)
                    {
                        m_grabbable.RemoveParent(this.gameObject);
                        m_grabbable = null;
                    }
                }
                else
                {
                    if (grabbDown)
                    {
                        //
                        // Grip
                        //

                        GameObject target = m_raycastHit.collider.gameObject;
                        TLabVRGrabbable grabbable = target.GetComponent<TLabVRGrabbable>();

                        if (grabbable == null) return;
                        if (grabbable.AddParent(this.gameObject) == true) m_grabbable = grabbable;
                    }
                }
            }
            else if (m_grabbable && !grip)
            {
                m_grabbable.RemoveParent(this.gameObject);
                m_grabbable = null;
            }
            else
            {
                if (Physics.Raycast(m_hand.PointerPose.position, m_hand.PointerPose.forward, out m_raycastHit, m_laserPointer.maxLength, m_layerMask))
                {

                    GameObject target = m_raycastHit.collider.gameObject;

                    //
                    // Outline
                    //

                    TLabOutlineSelectable selectable = target.GetComponent<TLabOutlineSelectable>();
                    if (selectable != null) selectable.Selected = true;

                    //
                    // PointerOn
                    //

                    Animator animator = target.GetComponent<Animator>();
                    if (animator != null) animator.SetBool("PointerOn", true);

                    //
                    // Rotate
                    //

                    TLabVRRotatable rotatable = target.GetComponent<TLabVRRotatable>();
                    if (rotatable == null) return;

                    if (m_hand.GetFingerIsPinching(OVRHand.HandFinger.Index))
                    {
                        Vector3 current = HandAngulerVelocity;
                        rotatable.SetHandAngulerVelocity(current.normalized, current.magnitude * 100f);
                    }
                }
            }
        }
    }
}
