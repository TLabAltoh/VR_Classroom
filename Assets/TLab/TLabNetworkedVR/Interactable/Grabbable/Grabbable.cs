using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using static TLab.XR.ComponentExtention;

namespace TLab.XR.Interact
{
    public class Grabbable : Interactable
    {
        public const int PARENT_LENGTH = 2;

        [Header("Rigidbody settings")]
        [SerializeField] private bool m_useRigidbody = true;
        [SerializeField] private bool m_useGravity = false;

        [Header("Transform Module")]
        [SerializeField] private PositionLogic m_position;
        [SerializeField] private RotationLogic m_rotation;
        [SerializeField] private ScaleLogic m_scale;

        [Header("Divided Settings")]
        [SerializeField] protected bool m_enableDivide = false;
        [SerializeField] protected GameObject[] m_divideTargets;

        private TLabXRHand m_mainHand;
        private TLabXRHand m_subHand;

        private Rigidbody m_rb;
        private bool m_gravityState = false;

#if UNITY_EDITOR
        // Windows 12's Core i 9: 400 -----> Size: 20
        private const int CASH_COUNT = 20;
#else
        // Oculsu Quest 2: 72 -----> Size: 20 * 72 / 400 = 3.6 ~= 4
        private const int CASH_COUNT = 5;
#endif

        private FixedQueue<Vector3> m_prebVels = new FixedQueue<Vector3>(CASH_COUNT);
        private FixedQueue<Vector3> m_prebArgs = new FixedQueue<Vector3>(CASH_COUNT);
        private List<CashTransform> m_cashTransforms = new List<CashTransform>();

        public PositionLogic position => m_position;

        public RotationLogic rotation => m_rotation;

        public ScaleLogic scale => m_scale;

        public TLabXRHand mainHand => m_mainHand;

        public TLabXRHand subHand => m_subHand;

        public bool grabbed { get => m_mainHand != null; }

        public bool enableDivide { get => m_enableDivide; }

        public GameObject[] divideTargets { get => m_divideTargets; }

        private string THIS_NAME { get => "[" + this.GetType().Name + "] "; }

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

        public void SetGravity(bool active)
        {
            if (m_rb == null || m_useRigidbody == false)
            {
                return;
            }

            active &= m_useGravity;
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

        private void MainHandGrabbStart()
        {
            m_position.OnMainHandGrabbed(m_mainHand);
            m_rotation.OnMainHandGrabbed(m_mainHand);
            m_scale.OnMainHandGrabbed(m_mainHand);
        }

        private void SubHandGrabbStart()
        {
            m_position.OnSubHandGrabbed(m_subHand);
            m_rotation.OnSubHandGrabbed(m_subHand);
            m_scale.OnSubHandGrabbed(m_subHand);
        }

        private void MainHandGrabbEnd()
        {
            m_position.OnMainHandReleased(m_mainHand);
            m_rotation.OnMainHandReleased(m_mainHand);
            m_scale.OnMainHandReleased(m_mainHand);
        }

        private void SubHandGrabbEnd()
        {
            m_position.OnSubHandReleased(m_subHand);
            m_rotation.OnSubHandReleased(m_subHand);
            m_scale.OnSubHandReleased(m_subHand);
        }

        private void IgnoreCollision(TLabXRHand hand, bool ignore)
        {
            //if (hand.physicsHand == null)
            //{
            //    return;
            //}

            //var jointPairs = hand.physicsHand.jointPairs;
            //foreach (JointPair jointPair in jointPairs)
            //{
            //    jointPair.slave.colliders.ForEach((c) => Physics.IgnoreCollision(c, m_collider, ignore));
            //}
        }

        public override void OnSelected(TLabXRHand hand)
        {
            if (m_mainHand == null)
            {
                RbGripSwitch(true);

                m_mainHand = hand;

                MainHandGrabbStart();

                IgnoreCollision(hand, true);

                Debug.Log(THIS_NAME + hand.ToString() + " mainHand added");
            }
            else if (m_subHand == null)
            {
                m_subHand = hand;

                SubHandGrabbStart();

                IgnoreCollision(hand, true);

                Debug.Log(THIS_NAME + hand.ToString() + " subHand added");
            }

            Debug.Log(THIS_NAME + "cannot add hand");

            base.OnSelected(hand);
        }

        public override void Unselected(TLabXRHand hand)
        {
            if (m_mainHand == hand)
            {
                MainHandGrabbEnd();

                IgnoreCollision(m_mainHand, false);

                if (m_subHand != null)
                {
                    m_mainHand = m_subHand;
                    m_subHand = null;

                    MainHandGrabbStart();

                    Debug.Log(THIS_NAME + "main released and sub added");
                }
                else
                {
                    RbGripSwitch(false);

                    m_mainHand = null;

                    Debug.Log(THIS_NAME + "main released");
                }
            }
            else if (m_subHand == hand)
            {
                SubHandGrabbEnd();

                IgnoreCollision(m_subHand, false);

                m_subHand = null;

                MainHandGrabbStart();

                Debug.Log(THIS_NAME + "sub released");
            }

            base.Unselected(hand);
        }

        public override void WhileSelected(TLabXRHand hand)
        {
            base.WhileSelected(hand);
        }

        private void CreateCombineMeshCollider()
        {
            MeshFilter meshFilter = this.gameObject.RequireComponent<MeshFilter>();

            MeshFilter[] meshFilters = GetComponentsInTargets<MeshFilter>(divideTargets);

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

        private void Devide(bool active)
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

            MeshCollider[] childs = GetComponentsInTargets<MeshCollider>(divideTargets);

            for (int i = 0; i < childs.Length; i++)
            {
                childs[i].enabled = active;
            }

            Rotatable[] rotatebles = this.gameObject.GetComponentsInChildren<Rotatable>();
            for (int i = 0; i < rotatebles.Length; i++)
            {
                //rotatebles[i].SetHandAngulerVelocity(Vector3.zero, 0.0f);
            }

            if (!active)
            {
                CreateCombineMeshCollider();
            }
        }

        private int Devide()
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

        private void GetInitialChildTransform()
        {
            m_cashTransforms.Clear();
            Transform[] childTransforms = GetComponentsInTargets<Transform>(divideTargets);
            foreach (Transform childTransform in childTransforms)
            {
                m_cashTransforms.Add(new CashTransform(
                    childTransform.localPosition,
                    childTransform.localScale,
                    childTransform.localRotation));
            }
        }

        private void SetInitialChildTransform()
        {
            if (!m_enableDivide)
            {
                return;
            }

            int index = 0;

            Transform[] childTransforms = GetComponentsInTargets<Transform>(divideTargets);
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

        private void CashRbVelocity()
        {
            if (m_rb != null)
            {
                m_prebVels.Enqueue(m_rb.velocity);
                m_prebArgs.Enqueue(m_rb.angularVelocity);
            }
        }

        private void Start()
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

            m_position.Start(this.transform, m_rb);
            m_rotation.Start(this.transform, m_rb);
            m_scale.Start(this.transform, m_rb);
        }

        private void Update()
        {
            CashRbVelocity();

            if (m_mainHand != null)
            {
                if (m_subHand != null)
                {
                    // This object is grabbed from two or more hands
                    m_position.UpdateTwoHandLogic();
                    m_scale.UpdateTwoHandLogic();
                }
                else
                {
                    // This object is grabbed from one hand
                    m_position.UpdateOneHandLogic();
                    m_rotation.UpdateOneHandLogic();
                }
            }
            else
            {
                m_scale.UpdateOneHandLogic();
            }
        }
    }
}
