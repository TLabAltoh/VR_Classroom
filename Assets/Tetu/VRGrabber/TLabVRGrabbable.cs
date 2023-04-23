using UnityEngine;

public class TLabVRGrabbable : MonoBehaviour
{
    public const int PARENT_LENGTH = 2;

    [Header("Rigidbody")]
    [SerializeField] protected bool m_useRigidbody = true;
    [SerializeField] protected bool m_useGravity = false;

    [Header("Transform fix")]
    [SerializeField] protected bool m_positionFixed = true;
    [SerializeField] protected bool m_rotateFixed = true;
    [SerializeField] protected bool m_scaling = true;

    [Header("Scaling Factor")]
    [SerializeField, Range(0.0f, 0.25f)] protected float m_scalingFactor;

    protected GameObject m_mainParent;
    protected GameObject m_subParent;

    protected Vector3 m_mainPositionOffset;
    protected Vector3 m_subPositionOffset;

    protected Quaternion m_mainQuaternionStart;
    protected Quaternion m_thisQuaternionStart;

    protected Rigidbody m_rb;

    protected float m_scaleInitialDistance = -1.0f;
    protected float m_parentScalingFactor;
    protected Vector3 m_scaleInitial;

    public bool Grabbed
    {
        get
        {
            return m_mainParent != null;
        }
    }

    protected virtual void EnableGravity(bool active)
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

    protected virtual void RbGripSwitch(bool grip)
    {
        if (m_useGravity == true)
        {
            EnableGravity(!grip);
        }
    }

    protected virtual void MainParentGrabbStart()
    {
        m_mainPositionOffset = m_mainParent.transform.InverseTransformPoint(this.transform.position);

        m_mainQuaternionStart = m_mainParent.transform.rotation;
        m_thisQuaternionStart = this.transform.rotation;
    }

    protected virtual void SubParentGrabStart()
    {
        m_subPositionOffset = m_subParent.transform.InverseTransformPoint(this.transform.position);
    }

    public virtual bool AddParent(GameObject parent)
    {
        if (m_mainParent == null)
        {
            RbGripSwitch(true);

            m_mainParent = parent;

            MainParentGrabbStart();

            Debug.Log("tlabvrhand: " + parent.ToString() + " mainParent added");
            return true;
        }
        else if(m_subParent == null)
        {
            m_subParent = parent;

            SubParentGrabStart();

            Debug.Log("tlabvrhand: " + parent.ToString() + " subParent added");
            return true;
        }

        Debug.Log("cannot add parent");
        return false;
    }

    public virtual bool RemoveParent(GameObject parent)
    {
        if(m_mainParent == parent)
        {
            if(m_subParent != null)
            {
                m_mainParent = m_subParent;
                m_subParent = null;

                MainParentGrabbStart();

                Debug.Log("tlabvrhand: " + "m_main released and m_sub added");

                return true;
            }
            else
            {
                RbGripSwitch(false);

                m_mainParent = null;

                Debug.Log("tlabvrhand: " + "m_main released");

                return true;
            }
        }
        else if(m_subParent == parent)
        {
            m_subParent = null;

            MainParentGrabbStart();

            Debug.Log("tlabvrhand: m_sub released");

            return true;
        }

        return false;
    }

    protected virtual void UpdateScale()
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
                m_rb.MovePosition(positionMain * 0.5f + positionSub * 0.5f);
            else
                this.transform.position = positionMain * 0.5f + positionSub * 0.5f;
        }
    }

    protected virtual void UpdatePosition()
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

    protected virtual void Start()
    {
        if(m_useRigidbody == true)
        {
            m_rb = GetComponent<Rigidbody>();
            if(m_rb == null)
                m_rb = this.gameObject.AddComponent<Rigidbody>();

            EnableGravity(m_useGravity);
        }

        m_parentScalingFactor = 1 - m_scalingFactor;
    }

    protected virtual void Update()
    {
        if(m_mainParent != null)
        {
            if(m_subParent != null && m_scaling == true)
            {
                UpdateScale();
            }
            else
            {
                m_scaleInitialDistance = -1.0f;

                UpdatePosition();
            }
        }
        else
        {
            m_scaleInitialDistance = -1.0f;
        }
    }
}
