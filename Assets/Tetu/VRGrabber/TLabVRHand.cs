using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabVRHand : MonoBehaviour
{
    [SerializeField] private OVRInput.Controller m_controller;
    [SerializeField] private LaserPointer m_laserPointer;
    [SerializeField] private OVRInput.Axis1D m_grip;
    [SerializeField] private float m_maxDistance = 10.0f;
    [SerializeField] private LayerMask m_layerMask;

    private Transform m_anchor;
    private OVRCameraRig m_cameraRig;
    private TLabVRGrabbable m_grabbable;
    private bool m_handInitialized = false;

    //
    // Raycast Info
    //

    private GameObject m_raycastResult = null;
    private RaycastHit m_raycastHit;

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

    void Start()
    {
        m_handInitialized = true;
        m_cameraRig = FindObjectOfType<OVRCameraRig>();

        if(m_controller == OVRInput.Controller.RTouch)
        {
            m_anchor = m_cameraRig.rightHandAnchor;
        }
        else if(m_controller == OVRInput.Controller.LTouch)
        {
            m_anchor = m_cameraRig.leftHandAnchor;
        }
        else
        {
            m_handInitialized = false;
            Debug.LogError("The controller type is not properly selected. Select RTouch or LTouch.");
        }
    }

    void Update()
    {
        if(m_handInitialized == false)
        {
            return;
        }

        if (m_grabbable)
        {
            bool grip = OVRInput.Get(m_grip, m_controller) > 0.5f;
            if (grip == false)
            {
                m_grabbable.RemoveParent(this.gameObject);

                m_grabbable = null;
            }
        }
        else
        {
            Ray ray = new Ray(m_anchor.position, m_anchor.forward);

            if (Physics.Raycast(ray, out m_raycastHit, m_maxDistance, m_layerMask))
            {
                GameObject target = m_raycastHit.collider.gameObject;

                m_raycastResult = target;

                if (m_raycastResult.GetComponent<Renderer>().material.HasProperty("_OutlineWidth"))
                {
                    m_raycastResult.GetComponent<Renderer>().material.SetFloat("_OutlineWidth", 0.025f);
                }

                // m_laserPointer.maxLength = (m_anchor.position - hit.point).magnitude;

                bool grip = OVRInput.Get(m_grip, m_controller) > 0.5f;
                if (grip)
                {
                    if (m_raycastResult.GetComponent<Renderer>().material.HasProperty("_OutlineWidth"))
                    {
                        m_raycastResult.GetComponent<Renderer>().material.SetFloat("_OutlineWidth", 0.0f);
                    }

                    TLabVRGrabbable grabbable = target.GetComponent<TLabVRGrabbable>();

                    if (grabbable == null)
                    {
                        return;
                    }

                    if (grabbable.AddParent(this.gameObject) == true)
                    {
                        m_grabbable = grabbable;
                    }
                }
            }
            else
            {
                if(m_raycastResult != null)
                {
                    if (m_raycastResult.GetComponent<Renderer>().material.HasProperty("_OutlineWidth"))
                    {
                        m_raycastResult.GetComponent<Renderer>().material.SetFloat("_OutlineWidth", 0.0f);
                    }
                }

                m_raycastResult = null;
                // m_laserPointer.maxLength = this.m_maxDistance;
            }
        }
    }
}
