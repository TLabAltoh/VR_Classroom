using System.Text;
using UnityEngine;

public class TLabSyncGrabbable : TLabVRGrabbable
{
    [Header("Sync Setting")]
    [SerializeField] public bool m_enableSync = false;
    [SerializeField] public bool m_autoSync = false;
    [SerializeField] public bool m_locked = false;

    [Header("World Initalize")]
    private bool m_rbAllocated = true;

    // https://www.fenet.jp/dotnet/column/language/4836/

    private StringBuilder builder = new StringBuilder();

    private bool CanRbSync
    {
        get
        {
            if(m_rb == null)
            {
                return false;
            }
            else
            {
                return m_rb.useGravity;
            }
        }
    }

    public bool UseGravity
    {
        get
        {
            return m_useGravity;
        }
    }

    public void SyncRemote(WebObjectInfo transform)
    {
        WebVector3 position = transform.position;
        WebVector3 scale = transform.scale;
        WebVector4 rotation = transform.rotation;

        this.transform.localScale = new Vector3(scale.x, scale.y, scale.z);

        if (m_useRigidbody == true)
        {
            m_rb.MovePosition(new Vector3(position.x, position.y, position.z));
            m_rb.MoveRotation(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
        }
        else
        {
            this.transform.position = new Vector3(position.x, position.y, position.z);
            this.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
        }
    }

    public void SetGravity(bool active)
    {
        if (m_rb != null)
        {
            EnableGravity(active);
        }
    }

    public void AllocateGravity(bool active)
    {
        m_rbAllocated = active;

        SetGravity(active);
    }

    public void ForceReleaseSelf()
    {
        if (m_mainParent != null)
        {
            m_mainParent = null;
            m_subParent = null;
            m_locked = false;

            RbGripSwitch(false);
        }
    }

    public void ForceReleaseRemote()
    {
        if(m_mainParent != null)
        {
            m_mainParent = null;
            m_subParent = null;
            m_locked = false;

            RbGripSwitch(false);
        }
    }

    public void ForceRelease()
    {
        ForceReleaseSelf();

        TLabSyncJson obj = new TLabSyncJson
        {
            role = (int)WebRole.guest,
            action = (int)WebAction.forceRelease,
            transform = new WebObjectInfo
            {
                id = this.gameObject.name
            }
        };
        string json = JsonUtility.ToJson(obj);
        TLabSyncClient.Instalce.SendWsMessage(json);

        Debug.Log("tlabvrhand: " + "force release");
    }

    public void GrabbLockSelf(bool active)
    {
        m_locked = active;
    }

    public void GrabbLockRemote(bool active)
    {
        if (active == true)
        {
            if (m_mainParent != null)
            {
                m_mainParent = null;
                m_subParent = null;
            }

            m_locked = true;
        }
        else
        {
            m_locked = false;
        }
    }

    public void GrabbLock(bool active)
    {
        TLabSyncJson obj = new TLabSyncJson
        {
            role = (int)WebRole.guest,
            action = (int)WebAction.grabbLock,
            active = active,
            transform = new WebObjectInfo
            {
                id = this.gameObject.name
            }
        };
        string json = JsonUtility.ToJson(obj);
        TLabSyncClient.Instalce.SendWsMessage(json);

        Debug.Log("tlabvrhand: " + "grabb lock");
    }

    protected override void EnableGravity(bool active)
    {
        base.EnableGravity(active);
    }

    protected override void RbGripSwitch(bool grip)
    {
        if (m_rbAllocated == true && m_useGravity == true)
        {
            EnableGravity(!grip);
        }

        if (m_enableSync == true && m_useRigidbody == true && m_useGravity == true)
        {
            TLabSyncJson obj = new TLabSyncJson
            {
                role = (int)WebRole.guest,
                action = (int)WebAction.setGravity,
                active = !grip,
                transform = new WebObjectInfo
                {
                    id = this.gameObject.name
                }
            };
            string json = JsonUtility.ToJson(obj);
            TLabSyncClient.Instalce.SendWsMessage(json);

            Debug.Log("tlabvrhand: " + "set gravity");
        }
    }

    protected override void MainParentGrabbStart()
    {
        base.MainParentGrabbStart();
    }

    protected override void SubParentGrabStart()
    {
        base.SubParentGrabStart();
    }

    public override bool AddParent(GameObject parent)
    {
        if(m_locked == true)
        {
            return false;
        }

        if (m_mainParent == null)
        {
            RbGripSwitch(true);

            m_mainParent = parent;

            MainParentGrabbStart();

            Debug.Log("tlabvrhand: " + parent.ToString() + " mainParent added");

            GrabbLock(true);

            return true;
        }
        else if (m_subParent == null)
        {
            m_subParent = parent;

            SubParentGrabStart();

            Debug.Log("tlabvrhand: " + parent.ToString() + " subParent added");
            return true;
        }

        Debug.Log("tlabvrhand: cannot add parent");
        return false;
    }

    public override bool RemoveParent(GameObject parent)
    {
        if (m_mainParent == parent)
        {
            if (m_subParent != null)
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

                GrabbLock(false);

                return true;
            }
        }
        else if (m_subParent == parent)
        {
            m_subParent = null;

            MainParentGrabbStart();

            Debug.Log("tlabvrhand: m_sub released");

            return true;
        }

        return false;
    }

    public void SyncTransform()
    {
        if (m_enableSync == false)
            return;

        // Optimized to reduce the amount of data

        builder.Clear();

        builder.Append("{");
            builder.Append(TLabSyncClientConst.ROLE);
            builder.Append(((int)WebRole.guest).ToString());
            builder.Append(TLabSyncClientConst.COMMA);
            
            builder.Append(TLabSyncClientConst.ACTION);
            builder.Append(((int)WebAction.syncTransform).ToString());
            builder.Append(TLabSyncClientConst.COMMA);
            
            builder.Append(TLabSyncClientConst.TRANSFORM);
            builder.Append("{");
                builder.Append(TLabSyncClientConst.TRANSFORM_ID);
                builder.Append("\"");
                builder.Append(this.gameObject.name);
                builder.Append("\"");
                builder.Append(TLabSyncClientConst.COMMA);
                
                builder.Append(TLabSyncClientConst.RIGIDBODY);
                builder.Append((m_useRigidbody ? "true" : "false"));
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.GRAVITY);
                builder.Append((m_useGravity ? "true" : "false"));
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.POSITION);
                builder.Append("{");
                    builder.Append(TLabSyncClientConst.X);
                    builder.Append((this.transform.position.x).ToString());
                    builder.Append(TLabSyncClientConst.COMMA);

                    builder.Append(TLabSyncClientConst.Y);
                    builder.Append((this.transform.position.y).ToString());
                    builder.Append(TLabSyncClientConst.COMMA);

                    builder.Append(TLabSyncClientConst.Z);
                    builder.Append((this.transform.position.z).ToString());
                builder.Append("}");
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.ROTATION);
                builder.Append("{");
                    builder.Append(TLabSyncClientConst.X);
                    builder.Append((this.transform.rotation.x).ToString());
                    builder.Append(TLabSyncClientConst.COMMA);

                    builder.Append(TLabSyncClientConst.Y);
                    builder.Append((this.transform.rotation.y).ToString());
                    builder.Append(TLabSyncClientConst.COMMA);

                    builder.Append(TLabSyncClientConst.Z);
                    builder.Append((this.transform.rotation.z).ToString());
                    builder.Append(TLabSyncClientConst.COMMA);

                    builder.Append(TLabSyncClientConst.W);
                    builder.Append((this.transform.rotation.w).ToString());
                builder.Append("}");
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.SCALE);
                builder.Append("{");
                    builder.Append(TLabSyncClientConst.X);
                    builder.Append((this.transform.localScale.x).ToString());
                    builder.Append(TLabSyncClientConst.COMMA);

                    builder.Append(TLabSyncClientConst.Y);
                    builder.Append((this.transform.localScale.y).ToString());
                    builder.Append(TLabSyncClientConst.COMMA);

                    builder.Append(TLabSyncClientConst.Z);
                    builder.Append((this.transform.localScale.z).ToString());
                builder.Append("}");
            builder.Append("}");
        builder.Append("}");

        string json = builder.ToString();

        //TLabSyncJson obj = new TLabSyncJson
        //{
        //    role = (int)WebRole.guest,
        //    action = (int)WebAction.syncTransform,

        //    transform = new WebObjectInfo
        //    {
        //        id = this.gameObject.name,

        //        rigidbody = m_useRigidbody,
        //        gravity = m_useGravity,

        //        position = new WebVector3
        //        {
        //            x = this.transform.position.x,
        //            y = this.transform.position.y,
        //            z = this.transform.position.z
        //        },
        //        rotation = new WebVector4
        //        {
        //            x = this.transform.rotation.x,
        //            y = this.transform.rotation.y,
        //            z = this.transform.rotation.z,
        //            w = this.transform.rotation.w,
        //        },
        //        scale = new WebVector3
        //        {
        //            x = this.transform.localScale.x,
        //            y = this.transform.localScale.y,
        //            z = this.transform.localScale.z
        //        }
        //    }
        //};

        //string json = JsonUtility.ToJson(obj);

        TLabSyncClient.Instalce.SendWsMessage(json);
    }

    protected override void UpdateScale()
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

            SyncTransform();
        }
    }

    protected override void UpdatePosition()
    {
        base.UpdatePosition();

        SyncTransform();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        if (m_mainParent != null)
        {
            if (m_subParent != null && m_scaling == true)
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

            if(m_enableSync == true && (m_autoSync == true || m_rbAllocated == true && CanRbSync == true))
            {
                SyncTransform();
            }
        }
    }
}
