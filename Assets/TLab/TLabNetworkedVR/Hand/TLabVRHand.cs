using UnityEngine;
using TLab.XR.VRGrabber.VFX;

namespace TLab.XR.VRGrabber
{
    public class TLabVRHand : MonoBehaviour
    {
        [Header("Controller Settings")]

        [Tooltip("OVR controller controlled by this hand (LTouch or RTouch)")]
        [SerializeField] private OVRInput.Controller m_controller;

        [Tooltip("This controller-controlled laser pointer")]
        [SerializeField] private LaserPointer m_laserPointer;

        [Tooltip("Maximum length of laser pointer")]
        [SerializeField] private float m_maxDistance = 10.0f;

        [Tooltip("Buttons on the controller used to grab objects")]
        [SerializeField] private OVRInput.Axis1D m_gripAxis;

        [Tooltip("Select the same button as GripAxis")]
        [SerializeField] private OVRInput.Button m_gripButton;

        [Tooltip("Specify the layer of the object you want to grab")]
        [SerializeField] private LayerMask m_layerMask;

        private Transform m_anchor;
        private OVRCameraRig m_cameraRig;
        private TLabVRGrabbable m_grabbable;
        private float m_laserPointerMaxLength;
        private bool m_handInitialized = false;

        //
        // Raycast Info
        //

        private GameObject m_raycastResult = null;
        private RaycastHit m_raycastHit;

        //
        private const string THIS_NAME = "[tlabvrhand] ";

        public GameObject RaycstResult
        {
            get
            {
                return m_raycastResult;
            }
        }

        public RaycastHit RaycastHit
        {
            get
            {
                return m_raycastHit;
            }
        }

        public TLabVRGrabbable CurrentGrabbable
        {
            get
            {
                return m_grabbable;
            }
        }

        void Start()
        {
            m_handInitialized = true;
            m_cameraRig = FindObjectOfType<OVRCameraRig>();
            m_laserPointerMaxLength = m_laserPointer.maxLength;

            if (m_controller == OVRInput.Controller.RTouch)
            {
                m_anchor = m_cameraRig.rightHandAnchor;
            }
            else if (m_controller == OVRInput.Controller.LTouch)
            {
                m_anchor = m_cameraRig.leftHandAnchor;
            }
            else
            {
                m_handInitialized = false;
                Debug.LogError(THIS_NAME + "The controller type is not properly selected. Select RTouch or LTouch.");
            }
        }

        void Update()
        {
            if (!m_handInitialized)
            {
                return;
            }

            Ray ray = new Ray(m_anchor.position, m_anchor.forward);

            if (Physics.Raycast(ray, out m_raycastHit, m_maxDistance, m_layerMask))
            {
                m_laserPointer.maxLength = m_raycastHit.distance;

                if (m_grabbable)
                {
                    bool grip = OVRInput.Get(m_gripAxis, m_controller) > 0.0f;
                    if (!grip)
                    {
                        m_grabbable.RemoveParent(this.gameObject);
                        m_grabbable = null;
                    }
                }
                else
                {
                    GameObject target = m_raycastHit.collider.gameObject;
                    m_raycastResult = target;

                    //
                    // Outline
                    //

                    var selectable = target.GetComponent<OutlineSelectable>();
                    if (selectable != null)
                    {
                        selectable.Selected = true;
                    }

                    //
                    // Grip
                    //

                    bool grip = OVRInput.GetDown(m_gripButton, m_controller);
                    if (grip)
                    {
                        var grabbable = target.GetComponent<TLabVRGrabbable>();

                        if (grabbable == null)
                        {
                            return;
                        }

                        if (grabbable.AddParent(this.gameObject))
                        {
                            m_grabbable = grabbable;
                        }
                    }
                }
            }
            else
            {
                m_laserPointer.maxLength = m_laserPointerMaxLength;

                if (m_grabbable)
                {
                    bool grip = OVRInput.Get(m_gripAxis, m_controller) > 0.0f;
                    if (!grip)
                    {
                        m_grabbable.RemoveParent(this.gameObject);
                        m_grabbable = null;
                    }
                }

                m_raycastResult = null;
            }
        }
    }
}
