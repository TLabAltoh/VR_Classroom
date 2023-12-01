using System.Collections;
using UnityEngine;
using UnityEditor;
using TLab.XR.Network;
using static TLab.XR.ComponentExtention;

namespace TLab.XR.Interact
{
    public class Grabbable : NetworkedObject
    {
        public const int PARENT_LENGTH = 2;

        public const int FREE = -1;
        public const int FIXED = -2;

        // Rigidbodyの同期にラグがあるとき，メッセージが届かない間はGravityを有効にしてローカルの環境で物理演算を行う．
        // ただし，誰かがオブジェクトを掴んでいることが分かっているときは，推測の物理演算は行わない．

        // Windows 12's Core i 9: 400 -----> Size: 10
        // Oculsu Quest 2: 72 -----> Size: 10 * 72 / 400 = 1.8 ~= 2

#if UNITY_EDITOR
        private const int PACKET_LOSS_LIMIT = 10;
#elif UNITY_ANDROID
        private const int PACKET_LOSS_LIMIT = 2;
#endif

        [Header("Transform Module")]
        [SerializeField] private PositionLogic m_position;
        [SerializeField] private RotationLogic m_rotation;
        [SerializeField] private ScaleLogic m_scale;

        [Header("Divided Settings")]
        [SerializeField] protected bool m_enableDivide = false;
        [SerializeField] protected GameObject[] m_divideTargets;

        private TLabXRHand m_mainHand;
        private TLabXRHand m_subHand;

        private int m_grabbed = FREE;

        public PositionLogic position => m_position;

        public RotationLogic rotation => m_rotation;

        public ScaleLogic scale => m_scale;

        public TLabXRHand mainHand => m_mainHand;

        public TLabXRHand subHand => m_subHand;

        public bool grabbed => m_mainHand != null;

        public int grabbedIndex => m_grabbed;

        public bool enableDivide => m_enableDivide;

        public GameObject[] divideTargets => m_divideTargets;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

#region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static new Hashtable registry => m_registry;

        public static void Register(string id, Grabbable grabbable) => m_registry[id] = grabbable;

        public static new void UnRegister(string id) => m_registry.Remove(id);

        public static new void ClearRegistry()
        {
            foreach (DictionaryEntry entry in m_registry)
            {
                var grabbable = entry.Value as Grabbable;
                grabbable.Shutdown(false);
            }

            m_registry.Clear();
        }

        public static new Grabbable GetById(string id) => m_registry[id] as Grabbable;

#endregion REGISTRY

#if UNITY_EDITOR
        public void InitializeRotatable()
        {
            if (EditorApplication.isPlaying)
            {
                return;
            }

            m_useGravity = false;
        }
#endif

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
            // TODO: add physics base hand ...

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

        public void AllocateGravity(bool active)
        {
            m_rbAllocated = active;

            bool allocated = m_grabbed == FREE && active;

            SetGravity(allocated ? true : false);

            Debug.Log(THIS_NAME + "rigidbody allocated:" + allocated + " - " + m_id);
        }

        public void GrabbLock(bool active)
        {
            m_grabbed = active ? SyncClient.Instance.seatIndex : FREE;

            if (m_rbAllocated)
            {
                SetGravity(!active);
            }

            SyncTransform();

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.GRABBLOCK,
                seatIndex: m_grabbed,
                transform: new WebObjectInfo { id = m_id });

            Debug.Log(THIS_NAME + "grabb lock: " + active);
        }

        public void GrabbLock(int index)
        {
            if (index != FREE)
            {
                if (m_mainHand != null)
                {
                    m_mainHand = null;
                    m_subHand = null;
                }

                m_grabbed = index;

                if (m_rbAllocated)
                {
                    SetGravity(false);
                }
            }
            else
            {
                m_grabbed = FREE;

                if (m_rbAllocated)
                {
                    SetGravity(true);
                }
            }
        }

        public void ForceRelease(bool self)
        {
            if (m_mainHand != null)
            {
                m_mainHand = null;
                m_subHand = null;
                m_grabbed = FREE;

                SetGravity(false);
            }

            if (self)
            {
                SyncClient.Instance.SendWsMessage(
                    role: WebRole.GUEST,
                    action: WebAction.FORCERELEASE,
                    transform: new WebObjectInfo { id = m_id });
            }

            Debug.Log(THIS_NAME + "force release");
        }

        public override void Selected(TLabXRHand hand)
        {
            if (m_mainHand == null)
            {
                SetGravity(!true);

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

            base.Selected(hand);
        }

        public override void UnSelected(TLabXRHand hand)
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
                    SetGravity(!true);

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

            base.UnSelected(hand);
        }

        public override void WhileSelected(TLabXRHand hand)
        {
            base.WhileSelected(hand);
        }

        private void CreateCombineMeshCollider()
        {
            var meshFilter = this.gameObject.RequireComponent<MeshFilter>();

            var meshFilters = GetComponentsInTargets<MeshFilter>(divideTargets);

            var combine = new CombineInstance[meshFilters.Length];

            for (int i = 0; i < meshFilters.Length; i++)
            {
                combine[i].mesh = meshFilters[i].sharedMesh;
                combine[i].transform = this.gameObject.transform.worldToLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
            }

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine);
            meshFilter.sharedMesh = mesh;

            var meshCollider = this.gameObject.RequireComponent<MeshCollider>();
            meshCollider.sharedMesh = meshFilter.sharedMesh;
        }

        public void Divide(bool active)
        {
            if (!m_enableDivide)
            {
                return;
            }

            var meshCollider = this.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                return;
            }

            meshCollider.enabled = !active;

            // TODO: GetComponentsInChildren以外のコレクションの収集方法を検討する

            var childs = GetComponentsInTargets<MeshCollider>(divideTargets);
            foreach (var child in childs)
            {
                child.enabled = active;
            }

            // 結合/分割を切り替えたので，divideTargetsが持つRotetableの回転を止める
            var rotatables = this.gameObject.GetComponentsInChildren<Rotatable>();
            foreach (var rotatable in rotatables)
            {
                rotatable.Stop();
            }

            // 結合/分割を切り替えたので，divideTargetsが持つGrabbableを含めて
            // Grabbableを誰も掴んでいない状態にする
            var grabbables = GetComponentsInTargets<Grabbable>(divideTargets);
            foreach (var grabbable in grabbables)
            {
                grabbable.ForceRelease(true);
            }

            if (!active)
            {
                CreateCombineMeshCollider();
            }
        }

        public void Devide()
        {
            if (!m_enableDivide)
            {
                return;
            }

            var meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                return;
            }

            // rootのMeshColliderが有効になっている --> 
            // rootのMeshColliderはdivideTargetsを結合したメッシュのコライダー -->
            // 現在このGrabbableは各パーツが結合されている状態
            var current = meshCollider.enabled;
            var divide = current;

            Divide(divide);
        }

        private void SetInitialChildTransform()
        {
            if (!m_enableDivide)
            {
                return;
            }

            int index = 0;

            var childTransforms = GetComponentsInTargets<Transform>(divideTargets);
            foreach (var childTransform in childTransforms)
            {
                var cashTransform = m_cashTransforms[index++];

                childTransform.localPosition = cashTransform.LocalPosiiton;
                childTransform.localRotation = cashTransform.LocalRotation;
                childTransform.localScale = cashTransform.LocalScale;
            }

            var meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                return;
            }

            if (meshCollider.enabled)
            {
                CreateCombineMeshCollider();
            }
        }

        private void GetInitialChildTransform()
        {
            if (m_enableDivide)
            {
                m_cashTransforms.Clear();

                var childTransforms = GetComponentsInTargets<Transform>(divideTargets);
                foreach (var childTransform in childTransforms)
                {
                    m_cashTransforms.Add(new CashTransform(
                        childTransform.localPosition,
                        childTransform.localScale,
                        childTransform.localRotation));
                }

                CreateCombineMeshCollider();
            }
        }

        protected override void Start()
        {
            base.Start();

            GetInitialChildTransform();

            m_position.Start(this.transform, m_rb);
            m_rotation.Start(this.transform, m_rb);
            m_scale.Start(this.transform, m_rb);

            Register(m_id, this);
        }

        protected override void Update()
        {
            base.Update();

            // 条件
            // 1. 重力が有効化されている
            // 2. パケットが指定したフレーム連続して届かなかった

            if (m_useGravity && m_didnotReachCount > PACKET_LOSS_LIMIT)
            {
                // 条件
                // 1. grabbed == FREE       : 重力が有効化されているので，ルーム参加者の誰かが重力の計算を実行しているはず (ただしその結果が共有されていない)
                // 2. rbAllocated == false  : このオブジェクトの重力の計算が自分の担当ではない (誰かが計算している)
                // 3. gravityState == false : このデバイスは重力計算を実行していない (Rigidbody.useGravity == false)

                if (m_grabbed == FREE && !m_rbAllocated && !m_gravityState)
                {
                    SetGravity(true);

                    // gravityState == trueになるので，この処理は次のフレーム以降実行されない
                }
            }

            if (m_mainHand != null)
            {
                if (m_subHand != null)
                {
                    m_position.UpdateTwoHandLogic();
                    // TODO: rotationのTwoHandLogicも追加したい
                    m_scale.UpdateTwoHandLogic();
                }
                else
                {
                    m_position.UpdateOneHandLogic();
                    m_rotation.UpdateOneHandLogic();
                    // TODO: scaleにOneHandLogicは必要ないかも ...
                }
            }
            else
            {
                // ハンドルを掴んでオブジェクトのサイズを変更する処理
                m_scale.UpdateOneHandLogic();
            }

            SyncRTCTransform();
        }

        void OnDestroy()
        {
            Shutdown(false);
        }

        void OnApplicationQuit()
        {
            Shutdown(false);
        }
    }
}
