using System.Collections.Generic;
using System.Collections;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static TLab.XR.ComponentExtention;

namespace TLab.XR.VRGrabber
{
    public class TLabVRGrabbable : MonoBehaviour
    {
        public const int PARENT_LENGTH = 2;

        [Header("Rigidbody Setting")]

        [Tooltip("Rigidbodyを使用するか")]
        [SerializeField] protected bool m_useRigidbody = true;

        [Tooltip("RigidbodyのUseGravityを有効化するか")]
        [SerializeField] protected bool m_useGravity = false;

        [Header("Transform update settings")]

        [Tooltip("掴んでいる間，オブジェクトのポジションを更新するか")]
        [SerializeField] protected bool m_positionFixed = true;

        [Tooltip("掴んでいる間，オブジェクトのローテーションを更新するか")]
        [SerializeField] protected bool m_rotateFixed = true;

        [Tooltip("両手で掴んでいる間，オブジェクトのスケールを更新するか")]
        [SerializeField] protected bool m_scaling = true;

        [Header("Scaling Factor")]
        [Tooltip("オブジェクトのスケールの更新の感度")]
        [SerializeField, Range(0.0f, 0.25f)] protected float m_scalingFactor;

        [Header("Divided Settings")]
        [Tooltip("このコンポーネントが子階層にGrabberを束ねているか")]
        [SerializeField] protected bool m_enableDivide = false;
        [SerializeField] protected GameObject[] m_divideTargets;

        protected GameObject m_mainParent;
        protected GameObject m_subParent;

        protected Vector3 m_mainPositionOffset;
        protected Vector3 m_subPositionOffset;

        protected Quaternion m_mainQuaternionStart;
        protected Quaternion m_thisQuaternionStart;

        protected Rigidbody m_rb;
        protected bool m_gravityState = false;
        // Windows 12's Core i 9: 400 -----> Size: 20
        // Oculsu Quest 2: 72 -----> Size: 20 * 72 / 400 = 3.6 ~= 4
#if UNITY_EDITOR
        protected FixedQueue<Vector3> m_prebVels = new FixedQueue<Vector3>(20);
        protected FixedQueue<Vector3> m_prebArgs = new FixedQueue<Vector3>(20);
#else
        protected FixedQueue<Vector3> m_prebVels = new FixedQueue<Vector3>(5);
        protected FixedQueue<Vector3> m_prebArgs = new FixedQueue<Vector3>(5);
#endif

        protected float m_scaleInitialDistance = -1.0f;
        protected float m_scalingFactorInvert;
        protected Vector3 m_scaleInitial;

        protected List<CashTransform> m_cashTransforms = new List<CashTransform>();

        private const string THIS_NAME = "[tlabvrgrabbable] ";

        public bool Grabbed { get => m_mainParent != null; }

        public bool EnableDivide { get => m_enableDivide; }

        public GameObject[] DivideTargets { get => m_divideTargets; }

#if UNITY_EDITOR
        public virtual void InitializeRotatable()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            m_useGravity = false;
        }

        public virtual void UseRigidbody(bool rigidbody, bool gravity)
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            m_useRigidbody = rigidbody;
            m_useGravity = gravity;
        }
#endif

        private Vector3 GetMaxVector(FixedQueue<Vector3> target)
        {
            Vector3 maxVec = Vector3.zero;
            float maxVecMag = 0.0f;
            foreach (Vector3 vec in target)
            {
                float vecMag = vec.magnitude;
                if (vecMag > maxVecMag)
                {
                    maxVecMag = vecMag;
                    maxVec = vec;
                }
            }

            return maxVec;
        }

        public virtual void SetGravity(bool active)
        {
            if (m_rb == null || m_useRigidbody == false || m_useGravity == false)
            {
                return;
            }

            m_gravityState = active;

            if (active)
            {
                m_rb.isKinematic = false;
                m_rb.useGravity = true;

                m_rb.velocity = GetMaxVector(m_prebVels);
                m_rb.angularVelocity = GetMaxVector(m_prebArgs);
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
            SetGravity(!grip);
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

                Debug.Log(THIS_NAME + parent.ToString() + " mainParent added");
                return true;
            }
            else if (m_subParent == null)
            {
                m_subParent = parent;

                SubParentGrabStart();

                Debug.Log(THIS_NAME + parent.ToString() + " subParent added");
                return true;
            }

            Debug.Log(THIS_NAME + "cannot add parent");
            return false;
        }

        public virtual bool RemoveParent(GameObject parent)
        {
            if (m_mainParent == parent)
            {
                if (m_subParent != null)
                {
                    m_mainParent = m_subParent;
                    m_subParent = null;

                    MainParentGrabbStart();

                    Debug.Log(THIS_NAME + "m_main released and m_sub added");

                    return true;
                }
                else
                {
                    RbGripSwitch(false);

                    m_mainParent = null;

                    Debug.Log(THIS_NAME + "m_main released");

                    return true;
                }
            }
            else if (m_subParent == parent)
            {
                m_subParent = null;

                MainParentGrabbStart();

                Debug.Log(THIS_NAME + "m_sub released");

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

            Vector3 scalingPositionMain = m_mainParent.transform.position * m_scalingFactorInvert + positionMain * m_scalingFactor;
            Vector3 scalingPositionSub = m_subParent.transform.position * m_scalingFactorInvert + positionSub * m_scalingFactor;

            if (m_scaleInitialDistance == -1.0f)
            {
                m_scaleInitialDistance = (scalingPositionMain - scalingPositionSub).magnitude;
                m_scaleInitial = this.transform.localScale;
            }
            else
            {
                float scaleRatio = (scalingPositionMain - scalingPositionSub).magnitude / m_scaleInitialDistance;
                this.transform.localScale = scaleRatio * m_scaleInitial;

                if (m_useRigidbody)
                {
                    m_rb.MovePosition(positionMain * 0.5f + positionSub * 0.5f);
                }
                else
                {
                    this.transform.position = positionMain * 0.5f + positionSub * 0.5f;
                }
            }
        }

        protected virtual void UpdatePosition()
        {
            if (m_useRigidbody)
            {
                if (m_positionFixed)
                {
                    m_rb.MovePosition(m_mainParent.transform.TransformPoint(m_mainPositionOffset));
                }

                if (m_rotateFixed)
                {
                    // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                    Quaternion deltaQuaternion = Quaternion.identity * m_mainParent.transform.rotation * Quaternion.Inverse(m_mainQuaternionStart);
                    m_rb.MoveRotation(deltaQuaternion * m_thisQuaternionStart);
                }
            }
            else
            {
                if (m_positionFixed)
                {
                    this.transform.position = m_mainParent.transform.TransformPoint(m_mainPositionOffset);
                }

                if (m_rotateFixed)
                {
                    // https://qiita.com/yaegaki/items/4d5a6af1d1738e102751
                    Quaternion deltaQuaternion = Quaternion.identity * m_mainParent.transform.rotation * Quaternion.Inverse(m_mainQuaternionStart);
                    this.transform.rotation = deltaQuaternion * m_thisQuaternionStart;
                }
            }
        }

        protected virtual void CreateCombineMeshCollider()
        {
            MeshFilter meshFilter = this.gameObject.RequireComponent<MeshFilter>();

            MeshFilter[] meshFilters = GetComponentsInTargets<MeshFilter>(DivideTargets);

            CombineInstance[] combine = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = this.gameObject.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            }

            Mesh mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine);
            meshFilter.sharedMesh = mesh;

            MeshCollider meshCollider = this.gameObject.RequireComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        protected virtual void Devide(bool active)
        {
            if (!m_enableDivide)
            {
                return;
            }

            MeshCollider meshCollider = this.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                return;
            }

            meshCollider.enabled = !active;

            MeshCollider[] childs = GetComponentsInTargets<MeshCollider>(DivideTargets);

            for (int i = 0; i < childs.Length; i++)
            {
                childs[i].enabled = active;
            }

            TLabVRRotatable[] rotatebles = this.gameObject.GetComponentsInChildren<TLabVRRotatable>();
            for (int i = 0; i < rotatebles.Length; i++)
            {
                rotatebles[i].SetHandAngulerVelocity(Vector3.zero, 0.0f);
            }

            if (!active)
            {
                CreateCombineMeshCollider();
            }
        }

        public virtual int Devide()
        {
            if (!m_enableDivide)
            {
                return -1;
            }

            MeshCollider meshCollider = this.gameObject.GetComponent<MeshCollider>();

            if (meshCollider == null)
            {
                return -1;
            }

            bool current = meshCollider.enabled;

            Devide(current);

            return current ? 0 : 1;
        }

        public virtual void GetInitialChildTransform()
        {
            m_cashTransforms.Clear();
            Transform[] childTransforms = GetComponentsInTargets<Transform>(DivideTargets);
            foreach (Transform childTransform in childTransforms)
            {
                m_cashTransforms.Add(new CashTransform(
                    childTransform.localPosition,
                    childTransform.localScale,
                    childTransform.localRotation));
            }
        }

        public virtual void SetInitialChildTransform()
        {
            if (!m_enableDivide)
            {
                return;
            }

            int index = 0;

            Transform[] childTransforms = GetComponentsInTargets<Transform>(DivideTargets);
            foreach (Transform childTransform in childTransforms)
            {
                CashTransform cashTransform = m_cashTransforms[index++];

                childTransform.localPosition = cashTransform.LocalPosiiton;
                childTransform.localRotation = cashTransform.LocalRotation;
                childTransform.localScale = cashTransform.LocalScale;
            }

            MeshCollider meshCollider = this.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                return;
            }

            if (meshCollider.enabled)
            {
                CreateCombineMeshCollider();
            }
        }

        protected virtual void CashRbVelocity()
        {
            if (m_rb != null)
            {
                m_prebVels.Enqueue(m_rb.velocity);
                m_prebArgs.Enqueue(m_rb.angularVelocity);
            }
        }

        protected virtual void Start()
        {
            if (m_enableDivide)
            {
                GetInitialChildTransform();
                CreateCombineMeshCollider();
            }

            if (m_useRigidbody)
            {
                m_rb = this.gameObject.RequireComponent<Rigidbody>();
                m_prebVels.Enqueue(m_rb.velocity);
                m_prebArgs.Enqueue(m_rb.angularVelocity);

                SetGravity(m_useGravity);
            }

            m_scalingFactorInvert = 1 - m_scalingFactor;
        }

        protected virtual void Update()
        {
            CashRbVelocity();

            if (m_mainParent != null)
            {
                if (m_subParent != null && m_scaling)
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
}