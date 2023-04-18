using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabVRGrabbable : MonoBehaviour
{
    public const int PARENT_LENGTH = 2;

    [Tooltip("Rigidbody")]
    [SerializeField] private bool m_useRigidbody = true;
    [SerializeField] private bool m_useGravity = false;

    [Tooltip("Transform fix")]
    [SerializeField] private bool m_positionFixed = true;
    [SerializeField] private bool m_rotateFixed = true;

    private GameObject m_mainParent;
    private GameObject m_subParent;

    private Vector3 m_mainPositionOffset;
    private Vector3 m_subPositionOffset;

    private Quaternion m_mainQuaternionStart;
    private Quaternion m_thisQuaternionStart;

    private Rigidbody m_rb;

    private void EnableGravity(bool active)
    {
        if (active == true)
        {
            m_rb.isKinematic = false;
            m_rb.useGravity = true;
        }
        else
        {
            m_rb.isKinematic = true;
            m_rb.useGravity = false;
            m_rb.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void RbGripSwitch(bool grip)
    {
        if (m_useGravity == true)
        {
            EnableGravity(!grip);
        }
    }

    public bool AddParent(GameObject parent)
    {
        if (m_mainParent == null)
        {
            RbGripSwitch(true);

            m_mainParent = parent;

            m_mainPositionOffset = parent.transform.InverseTransformPoint(this.transform.position);

            m_mainQuaternionStart = parent.transform.rotation;
            m_thisQuaternionStart = this.transform.rotation;

            Debug.Log("tlabvrhand:" + parent.ToString() + " mainParent added");
            return true;
        }
        else if(m_subParent == null)
        {
            m_subParent = parent;

            m_subPositionOffset = parent.transform.InverseTransformPoint(this.transform.position);

            Debug.Log("tlabvrhand:" + parent.ToString() + " subParent added");
            return true;
        }

        Debug.Log("cannot add parent");
        return false;
    }

    public bool RemoveParent(GameObject parent)
    {
        if(m_mainParent == parent)
        {
            if(m_subParent != null)
            {
                m_mainParent = m_subParent;
                m_subParent = null;

                m_mainPositionOffset = m_mainParent.transform.InverseTransformPoint(this.transform.position);

                m_mainQuaternionStart = m_mainParent.transform.rotation;
                m_thisQuaternionStart = this.transform.rotation;

                Debug.Log("tlabvrhand:" + "m_main released and m_sub added");

                return true;
            }
            else
            {
                RbGripSwitch(false);

                m_mainParent = null;

                Debug.Log("tlabvrhand:" + "m_main released");

                return true;
            }
        }
        else if(m_subParent == parent)
        {
            m_subParent = null;

            Debug.Log("m_sub released");

            return true;
        }

        return false;
    }

    void Start()
    {
        if(m_useRigidbody == true)
        {
            m_rb = GetComponent<Rigidbody>();
            if(m_rb == null)
            {
                m_rb = this.gameObject.AddComponent<Rigidbody>();
            }

            EnableGravity(m_useGravity);
        }
    }

    void Update()
    {
        if(m_mainParent != null)
        {
            if(m_subParent != null)
            {

            }
            else
            {
                if (m_useRigidbody == true)
                {
                    if (m_positionFixed == true)
                    {
                        m_rb.MovePosition(m_mainParent.transform.TransformPoint(m_mainPositionOffset));
                    }

                    if (m_rotateFixed == true)
                    {
                        // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                        Quaternion deltaQuaternion = Quaternion.identity * m_mainParent.transform.rotation * Quaternion.Inverse(m_mainQuaternionStart);
                        m_rb.MoveRotation(this.transform.rotation = deltaQuaternion * m_thisQuaternionStart);
                    }
                }
                else
                {
                    if (m_positionFixed == true)
                    {
                        this.transform.position = m_mainParent.transform.TransformPoint(m_mainPositionOffset);
                    }

                    if (m_rotateFixed == true)
                    {
                        // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                        Quaternion deltaQuaternion = Quaternion.identity * m_mainParent.transform.rotation * Quaternion.Inverse(m_mainQuaternionStart);
                        this.transform.rotation = deltaQuaternion * m_thisQuaternionStart;
                    }
                }
            }
        }
    }
}
