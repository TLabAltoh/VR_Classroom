using UnityEngine;

public class TLabSyncGrabbable : TLabVRGrabbable
{
    [SerializeField] public bool m_enableSync = false;

    // https://www.fenet.jp/dotnet/column/language/4836/

    protected override void EnableGravity(bool active)
    {
        base.EnableGravity(active);
    }

    protected override void RbGripSwitch(bool grip)
    {
        base.RbGripSwitch(grip);
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
        return base.AddParent(parent);
    }

    public override bool RemoveParent(GameObject parent)
    {
        return base.RemoveParent(parent);
    }

    public void SyncTransform()
    {
        if (m_enableSync == false)
            return;

        TLabSyncJson obj = new TLabSyncJson
        {
            role = "student",
            action = "sync transform",

            id = this.gameObject.name,

            positionX = this.transform.position.x,
            positionY = this.transform.position.y,
            positionZ = this.transform.position.z,

            rotationX = this.transform.rotation.x,
            rotationY = this.transform.rotation.y,
            rotationZ = this.transform.rotation.z,

            scaleX = this.transform.localScale.x,
            scaleY = this.transform.localScale.y,
            scaleZ = this.transform.localScale.z,

        };

        string json = JsonUtility.ToJson(obj);

        TLabSyncClient.Instalce.SendWsMessage(json);
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        if (m_mainParent != null)
        {
            if (m_subParent != null)
            {
                if (m_scaling == true)
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

                SyncTransform();
            }
        }
        else
        {
            m_scaleInitialDistance = -1.0f;
        }
    }
}
