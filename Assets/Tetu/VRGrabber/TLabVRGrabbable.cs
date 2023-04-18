using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabVRGrabbable : MonoBehaviour
{
    public const int PARENT_LENGTH = 2;

    public bool m_useRigidbody = true;

    private GameObject m_mainParent;
    private GameObject m_subParent;


    public bool AddParent(GameObject parent)
    {
        if (m_mainParent == null)
        {
            m_mainParent = parent;
            Debug.Log("tlabvrhand:" + parent.ToString() + " mainParent added");
            return true;
        }
        else if(m_subParent == null)
        {
            m_subParent = parent;
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

                Debug.Log("tlabvrhand:" + "m_main released and m_sub added");

                return true;
            }
            else
            {
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
        
    }

    void Update()
    {
        
    }
}
