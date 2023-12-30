using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;
using TLab.XR.Network;
using static TLab.XR.ComponentExtention;

namespace TLab.XR.Interact
{
    public class ExclusiveController : SyncTransformer
    {
        public enum HandType
        {
            MAIN_HAND,
            SUB_HAND,
            NONE
        };

        // Rigidbody�̓����Ƀ��O������Ƃ��C���b�Z�[�W���͂��Ȃ��Ԃ�Gravity��L���ɂ��ă��[�J���̊��ŕ������Z���s���D
        // �������C�N�����I�u�W�F�N�g��͂�ł��邱�Ƃ��������Ă���Ƃ��́C�����̕������Z�͍s��Ȃ��D

        // Windows 12's Core i 9: 400 -----> Size: 10
        // Oculsu Quest 2: 72 -----> Size: 10 * 72 / 400 = 1.8 ~= 2

#if UNITY_EDITOR
        private const int PACKET_LOSS_LIMIT = 10;
#elif UNITY_ANDROID
        private const int PACKET_LOSS_LIMIT = 2;
#endif

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static Hashtable registry => m_registry;

        protected static void Register(string id, ExclusiveController controller)
        {
            if (!m_registry.ContainsKey(id)) m_registry[id] = controller;
        }

        protected static new void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id)) m_registry.Remove(id);
        }

        public static new void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var controller = entry.Value as ExclusiveController;
                gameobjects.Add(controller.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static new ExclusiveController GetById(string id) => m_registry[id] as ExclusiveController;

        #endregion REGISTRY

        [Header("Exclusive Sync Settings")]
        [SerializeField] protected bool m_locked = false;

        [Header("Transform Module")]
        [SerializeField] private PositionLogic m_position;
        [SerializeField] private RotationLogic m_rotation;
        [SerializeField] private ScaleLogic m_scale;

        [Header("Divided Settings")]
        [SerializeField] protected bool m_enableDivide = false;
        [SerializeField] protected MeshCollider m_meshCollider;
        [SerializeField] protected GameObject[] m_divideTargets;

        protected List<CashTransform> m_cashTransforms = new List<CashTransform>();

        public const int FREE = -1;
        public const int FIXED = -2;

        private int m_grabbedIndex = FREE;

        private Interactor m_mainHand;
        private Interactor m_subHand;

        public bool grabbed => m_grabbedIndex != FREE && m_grabbedIndex != FIXED;

        public int grabbedIndex => m_grabbedIndex;

        public bool locked => m_locked;

        public bool isFree => m_grabbedIndex == FREE;

        public bool grabbByMe => grabbed && (m_grabbedIndex == SyncClient.Instance.seatIndex);

        public Interactor mainHand => m_mainHand;

        public Interactor subHand => m_subHand;

        public PositionLogic position => m_position;

        public RotationLogic rotation => m_rotation;

        public ScaleLogic scale => m_scale;

        public bool enableDivide => m_enableDivide;

        public GameObject[] divideTargets => m_divideTargets;

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

        public void AllocateGravity(bool active)
        {
            m_rbAllocated = active;

            bool enableGravity = m_grabbedIndex == FREE && active;

            SetGravity(enableGravity ? true : false);

            Debug.Log(THIS_NAME + "rigidbody allocated:" + m_rbAllocated + ", enableGravity: " + enableGravity + ", id:" + m_id);
        }

        public void GrabbLock(bool active)
        {
            m_grabbedIndex = active ? SyncClient.Instance.seatIndex : FREE;

            if (m_rbAllocated)
            {
                SetGravity(!active);
            }

            SyncTransform();

            SyncClient.Instance.SendWsMessage(
                action: WebAction.GRABBLOCK,
                grabIndex: m_grabbedIndex,
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

                m_grabbedIndex = index;

                if (m_rbAllocated)
                {
                    SetGravity(false);
                }
            }
            else
            {
                m_grabbedIndex = FREE;

                if (m_rbAllocated)
                {
                    SetGravity(true);
                }
            }
        }

        public void SimpleLock(bool active)
        {
            /*
                -1 : No one is grabbing
                -2 : No one grabbed, but Rigidbody does not calculate
            */

            // Ensure that the object you are grasping does not cover
            // If someone has already grabbed the object, overwrite it

            // parse.seatIndex	: player index that is grabbing the object
            // seatIndex		: index of the socket actually communicating

            m_grabbedIndex = active ? FIXED : FREE;

            if (m_rbAllocated)
            {
                SetGravity(!active);
            }

            SyncClient.Instance.SendWsMessage(
                action: WebAction.GRABBLOCK,
                grabIndex: m_grabbedIndex,
                transform: new WebObjectInfo { id = m_id });

            Debug.Log(THIS_NAME + "simple lock");
        }

        public void ForceRelease(bool self)
        {
            if (m_mainHand != null)
            {
                m_mainHand = null;
                m_subHand = null;
                m_grabbedIndex = FREE;

                SetGravity(false);
            }

            if (self)
            {
                SyncClient.Instance.SendWsMessage(
                    action: WebAction.FORCERELEASE,
                    transform: new WebObjectInfo { id = m_id });
            }
        }


        private void CreateCombineMeshCollider()
        {
            var meshColliders = GetComponentsInTargets<MeshCollider>(divideTargets);

            var combine = new CombineInstance[meshColliders.Length];

            for (int i = 0; i < meshColliders.Length; i++)
            {
                combine[i].mesh = meshColliders[i].sharedMesh;
                combine[i].transform = gameObject.transform.worldToLocalMatrix * meshColliders[i].transform.localToWorldMatrix;
            }

            var mesh = new Mesh();
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.CombineMeshes(combine);

            if (m_meshCollider != null)
                m_meshCollider.sharedMesh = mesh;
        }

        public void Divide(bool active)
        {
            if (!m_enableDivide) return;

            var meshCollider = this.gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError(THIS_NAME + "Mesh Collider Not Found");
                return;
            }

            meshCollider.enabled = !active;

            // TODO: GetComponentsInChildren�ȊO�̃R���N�V�����̎��W���@����������

            var childs = GetComponentsInTargets<MeshCollider>(divideTargets);
            foreach (var child in childs) child.enabled = active;

            // ����/������؂�ւ����̂ŁCdivideTargets������Rotetable�̉�]���~�߂�
            var rotatables = this.gameObject.GetComponentsInChildren<Rotatable>();
            foreach (var rotatable in rotatables) rotatable.Stop();

            // ����/������؂�ւ����̂ŁCdivideTargets������ExclusiveController���܂߂�
            // ExclusiveController��N���͂�ł��Ȃ���Ԃɂ���
            var controllers = GetComponentsInTargets<ExclusiveController>(divideTargets);
            foreach (var controller in controllers) controller.ForceRelease(true);

            if (!active) CreateCombineMeshCollider();
        }

        public void Devide()
        {
            if (!m_enableDivide) return;

            var meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError(THIS_NAME + "Mesh Collider Not Found");
                return;
            }

            // root��MeshCollider���L���ɂȂ��Ă��� --> 
            // root��MeshCollider��divideTargets�������������b�V���̃R���C�_�[ -->
            // ���݂���Grabbable�͊e�p�[�c����������Ă�����
            var current = meshCollider.enabled;
            var divide = current;

            Divide(divide);
        }

        public void SetInitialChildTransform()
        {
            if (!m_enableDivide) return;

            int index = 0;

            var childTransforms = GetComponentsInTargets<Transform>(divideTargets);
            foreach (var childTransform in childTransforms)
            {
                var cashTransform = m_cashTransforms[index++];

                childTransform.localPosition = cashTransform.LocalPosiiton;
                childTransform.localRotation = cashTransform.LocalRotation;
                childTransform.localScale = cashTransform.LocalScale;
            }

            var rotatables = this.gameObject.GetComponentsInChildren<Rotatable>();
            foreach (var rotatable in rotatables) rotatable.Stop();

            var meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null)
            {
                Debug.LogError(THIS_NAME + "Mesh Collider Not Found");
                return;
            }

            if (meshCollider.enabled) CreateCombineMeshCollider();
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

        public HandType GetHandType(Interactor interactor)
        {
            if (m_mainHand == interactor) return HandType.MAIN_HAND;

            if (m_subHand == interactor) return HandType.SUB_HAND;

            return HandType.NONE;
        }

        public HandType OnGrabbed(Interactor interactor)
        {
            if (m_locked || (!isFree && !grabbByMe)) return HandType.NONE;

            if (m_mainHand == null)
            {
                GrabbLock(true);

                m_mainHand = interactor;

                MainHandGrabbStart();

                return HandType.MAIN_HAND;
            }
            else if (m_subHand == null)
            {
                m_subHand = interactor;

                SubHandGrabbStart();

                return HandType.SUB_HAND;
            }

            return HandType.NONE;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="interactor"></param>
        /// <returns>Subhand promoted to maininteractor.</returns>
        public bool OnRelease(Interactor interactor)
        {
            if (m_mainHand == interactor)
            {
                MainHandGrabbEnd();

                if (m_subHand != null)
                {
                    m_mainHand = m_subHand;
                    m_subHand = null;

                    MainHandGrabbStart();

                    return true;
                }
                else
                {
                    GrabbLock(false);

                    m_mainHand = null;

                    return false;
                }
            }
            else if (m_subHand == interactor)
            {
                SubHandGrabbEnd();

                m_subHand = null;

                MainHandGrabbStart();

                return false;
            }

            return false;
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

            // ����
            // 1. �d�͂��L��������Ă���
            // 2. �p�P�b�g���w�肵���t���[���A�����ē͂��Ȃ�����

            if (m_useGravity && m_didnotReachCount > PACKET_LOSS_LIMIT)
            {
                // ����
                // 1. grabbed == FREE       : �d�͂��L��������Ă���̂ŁC���[���Q���҂̒N�����d�͂̌v�Z�����s���Ă���͂� (���������̌��ʂ����L����Ă��Ȃ�)
                // 2. rbAllocated == false  : ���̃I�u�W�F�N�g�̏d�͂̌v�Z�������̒S���ł͂Ȃ� (�N�����v�Z���Ă���)
                // 3. gravityState == false : ���̃f�o�C�X�͏d�͌v�Z�����s���Ă��Ȃ� (Rigidbody.useGravity == false)

                if (m_grabbedIndex == FREE && !m_rbAllocated && !m_gravityState)
                {
                    SetGravity(true);

                    // gravityState == true�ɂȂ�̂ŁC���̏����͎��̃t���[���ȍ~���s����Ȃ�
                }
            }

            if (m_mainHand != null)
            {
                if (m_subHand != null)
                {
                    m_position.UpdateTwoHandLogic();
                    // TODO: rotation��TwoHandLogic���ǉ�������
                    m_scale.UpdateTwoHandLogic();
                }
                else
                {
                    m_position.UpdateOneHandLogic();
                    m_rotation.UpdateOneHandLogic();
                    // TODO: scale��OneHandLogic�͕K�v�Ȃ����� ...
                }

                SyncRTCTransform();
            }
            else
            {
                // �n���h����͂�ŃI�u�W�F�N�g�̃T�C�Y��ύX���鏈��
                if (isFree && m_scale.UpdateHandleLogic())
                {
                    SyncRTCTransform();
                }
            }
        }

        private void Shutdown()
        {
            if (grabbByMe)
            {
                GrabbLock(false);
            }

            UnRegister(m_id);
        }

        protected override void OnDestroy()
        {
            Shutdown();

            base.OnDestroy();
        }

        protected override void OnApplicationQuit()
        {
            Shutdown();

            base.OnApplicationQuit();
        }
    }
}