using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabVRGrabbable : MonoBehaviour
{
    public const int PARENT_LENGTH = 2;

    [Header("Rigidbody")]
    [SerializeField] private bool m_useRigidbody = true;
    [SerializeField] private bool m_useGravity = false;

    [Header("Transform fix")]
    [SerializeField] private bool m_positionFixed = true;
    [SerializeField] private bool m_rotateFixed = true;
    [SerializeField] private bool m_scaling = true;

    [Header("Scaling Factor")]
    [SerializeField, Range(0.0f, 0.25f)] private float m_scalingFactor;

    private GameObject m_mainParent;
    private GameObject m_subParent;

    private Vector3 m_mainPositionOffset;
    private Vector3 m_subPositionOffset;

    private Quaternion m_mainQuaternionStart;
    private Quaternion m_thisQuaternionStart;

    private Rigidbody m_rb;

    private float m_scaleInitialDistance = -1.0f;
    private float m_parentScalingFactor;
    private Vector3 m_scaleInitial;

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

        m_parentScalingFactor = 1 - m_scalingFactor;
    }

    void Update()
    {
        if(m_mainParent != null)
        {
            if(m_subParent != null)
            {
                if(m_scaling == true)
                {
                    Vector3 positionMain = m_mainParent.transform.TransformPoint(m_mainPositionOffset);
                    Vector3 positionSub = m_subParent.transform.TransformPoint(m_subPositionOffset);

                    // この処理の最初の実行時，必ずpositionMainとpositionSubは同じ座標になる
                    // 拡縮の基準が小さくなりすぎてしまい，不都合
                    // ---> 手の位置に座標を補間して，2つの座標を意図的にずらす

                    float ratioParent = 1 - m_scalingFactor;
                    Vector3 scalingPositionMain = m_mainParent.transform.position * m_parentScalingFactor + positionMain * m_scalingFactor;
                    Vector3 scalingPositionSub = m_subParent.transform.position * m_parentScalingFactor + positionSub * m_scalingFactor;

                    if (m_scaleInitialDistance == -1.0f)
                    {
                        m_scaleInitialDistance = (scalingPositionMain - scalingPositionSub).magnitude;
                        m_scaleInitial = this.transform.localScale;
                    }
                    else
                    {
                        float scaleRatio = (scalingPositionMain - scalingPositionSub).magnitude / m_scaleInitialDistance;

                        this.transform.localScale = scaleRatio * m_scaleInitial;

                        if (m_useRigidbody == true)
                        {
                            m_rb.MovePosition(positionMain * 0.5f + positionSub * 0.5f);
                        }
                        else
                        {
                            this.transform.position = positionMain * 0.5f + positionSub * 0.5f;
                        }
                    }
                }
            }
            else
            {
                m_scaleInitialDistance = -1.0f;

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
                        m_rb.MoveRotation(deltaQuaternion * m_thisQuaternionStart);
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
        else
        {
            m_scaleInitialDistance = -1.0f;
        }
    }
}
