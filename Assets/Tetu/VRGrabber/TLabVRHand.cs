using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabVRHand : MonoBehaviour
{
    public OVRInput.Controller m_controller;
    public LaserPointer m_laserPointer;
    public OVRInput.Axis1D m_grip;
    public float m_maxDistance = 10.0f;
    public LayerMask m_layerMask;

    private Transform m_anchor;
    private OVRCameraRig m_cameraRig;
    private TLabVRGrabbable m_grabbable;
    private bool m_handInitialized = false;

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
            RaycastHit hit;
            Ray ray = new Ray(m_anchor.position, m_anchor.forward);

            if (Physics.Raycast(ray, out hit, m_maxDistance, m_layerMask))
            {
                GameObject target = hit.collider.gameObject;

                m_laserPointer.maxLength = (m_anchor.position - hit.point).magnitude;

                bool grip = OVRInput.Get(m_grip, m_controller) > 0.5f;
                if (grip)
                {
                    TLabVRGrabbable grabbable = target.GetComponent<TLabVRGrabbable>();

                    if (grabbable == null)
                    {
                        return;
                    }

                    grabbable.AddParent(this.gameObject);

                    m_grabbable = grabbable;
                }
            }
            else
            {
                m_laserPointer.maxLength = this.m_maxDistance;
            }
        }
    }
}
