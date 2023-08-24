using System.Collections;
using System.Text;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static TLab.XR.VRGrabber.Utility.ComponentExtention;

namespace TLab.XR.VRGrabber
{
    public class TLabSyncGrabbable : TLabVRGrabbable
    {
        [Header("Sync Setting")]

        [Tooltip("���ꂪ�L��������Ă��Ȃ��ƃI�u�W�F�N�g�͓�������Ȃ�")]
        [SerializeField] public bool m_enableSync = false;

        [Tooltip("�L��������Ɩ��t���[�������ŃI�u�W�F�N�g�𓯊�����")]
        [SerializeField] public bool m_autoSync = false;

        [Tooltip("�L��������ƒN��������̃I�u�W�F�N�g��͂߂Ȃ��Ȃ�")]
        [SerializeField] public bool m_locked = false;

        private bool m_rbAllocated = false;
        private int m_grabbed = -1;

        private bool m_isSyncFromOutside = false;
        private int m_didnotReachCount = 0;

        private bool m_shutdown = false;

        // https://www.fenet.jp/dotnet/column/language/4836/
        // A fast approach to string processing

        private StringBuilder builder = new StringBuilder();

        //
        private const string thisName = "[tlabsyncgrabbable] ";

        public bool IsEnableGravity
        {
            get
            {
                return (m_rb == null) ? false : m_rb.useGravity;
            }
        }

        public bool IsUseGravity
        {
            get
            {
                return m_useGravity;
            }
        }

        public int GrabbedIndex
        {
            get
            {
                return m_grabbed;
            }
        }

        public bool IsGrabbLocked
        {
            get
            {
                return m_grabbed != -1;
            }
        }

        public bool RbAllocated
        {
            get
            {
                return m_rbAllocated;
            }
        }

        private bool EnableAutoSync
        {
            get
            {
                return m_enableSync && !IsGrabbLocked && (m_autoSync || m_rbAllocated && IsEnableGravity);
            }
        }

        public bool IsSyncFromOutside
        {
            get
            {
                return m_isSyncFromOutside;
            }
        }

        private bool SocketIsOpen
        {
            get
            {
                return (TLabSyncClient.Instalce != null &&
                        TLabSyncClient.Instalce.SocketIsOpen == true &&
                        TLabSyncClient.Instalce.SeatIndex != -1);
            }
        }

        public void SyncFromOutside(WebObjectInfo transform)
        {
            WebVector3 position = transform.position;
            WebVector3 scale = transform.scale;
            WebVector4 rotation = transform.rotation;

            this.transform.localScale = new Vector3(scale.x, scale.y, scale.z);

            if (m_useRigidbody == true)
            {
                // Rigidbody�̑��x�𐳂����v�Z���邽�߂ɁCGravity������������Ă��邱�Ƃ��m�F����D
                SetGravity(false);

                m_rb.MovePosition(new Vector3(position.x, position.y, position.z));
                m_rb.MoveRotation(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
            }
            else
            {
                this.transform.position = new Vector3(position.x, position.y, position.z);
                this.transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            }

            m_isSyncFromOutside = true;
            m_didnotReachCount = 0;
        }

        private IEnumerator RegistRbObj()
        {
            // if useGravity is false, doesn't regist this object to server
            if (m_useGravity == false) yield break;

            // Wait for connection is opened
            while (SocketIsOpen == false) yield return null;

            TLabSyncClient.Instalce.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.REGISTRBOBJ,
                transform: new WebObjectInfo { id = this.gameObject.name });

            Debug.Log(thisName + "Send Rb Obj");
        }

        public void AllocateGravity(bool active)
        {
            m_rbAllocated = active;

            SetGravity((m_grabbed == -1 && active) ? true : false);

            bool allocated = m_grabbed == -1 && active;
            Debug.Log(thisName + "rb allocated:" + allocated + " - " + this.gameObject.name);
        }

        public void ForceReleaseSelf()
        {
            if (m_mainParent != null)
            {
                m_mainParent = null;
                m_subParent = null;
                m_grabbed = -1;

                SetGravity(false);
            }
        }

        public void ForceReleaseFromOutside()
        {
            if (m_mainParent != null)
            {
                m_mainParent = null;
                m_subParent = null;
                m_grabbed = -1;

                SetGravity(false);
            }
        }

        public void ForceRelease()
        {
            ForceReleaseSelf();

            TLabSyncClient.Instalce.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.FORCERELEASE,
                transform: new WebObjectInfo { id = this.gameObject.name });

            Debug.Log(thisName + "force release");
        }

        public void GrabbLockFromOutside(int index)
        {
            if (index != -1)
            {
                if (m_mainParent != null)
                {
                    m_mainParent = null;
                    m_subParent = null;
                }

                m_grabbed = index;

                if (m_rbAllocated == true)
                    SetGravity(false);
            }
            else
            {
                m_grabbed = -1;
                if (m_rbAllocated == true)
                    SetGravity(true);
            }
        }

        public void GrabbLock(bool active)
        {
            m_grabbed = active ? TLabSyncClient.Instalce.SeatIndex : -1;

            if (m_rbAllocated == true)
                SetGravity(!active);

            SyncTransform();

            TLabSyncClient.Instalce.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.GRABBLOCK,
                seatIndex: active ? TLabSyncClient.Instalce.SeatIndex : -1,
                transform: new WebObjectInfo { id = this.gameObject.name });

            Debug.Log(thisName + "grabb lock");
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

            m_grabbed = active ? -2 : -1;

            if (m_rbAllocated) SetGravity(!active);

            TLabSyncClient.Instalce.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.GRABBLOCK,
                seatIndex: active ? -2 : -1,
                transform: new WebObjectInfo { id = this.gameObject.name });

            Debug.Log(thisName + "simple lock");
        }

        protected override void RbGripSwitch(bool grip)
        {
            GrabbLock(grip);
        }

        public override bool AddParent(GameObject parent)
        {
            if (m_locked == true || m_grabbed != -1 && m_grabbed != TLabSyncClient.Instalce.SeatIndex)
                return false;

            return base.AddParent(parent);
        }

        #region SyncTransform
        private unsafe void LongCopy(byte* src, byte* dst, int count)
        {
            // https://github.com/neuecc/MessagePack-CSharp/issues/117

            while (count >= 8)
            {
                *(ulong*)dst = *(ulong*)src;
                dst += 8;
                src += 8;
                count -= 8;
            }
            if (count >= 4)
            {
                *(uint*)dst = *(uint*)src;
                dst += 4;
                src += 4;
                count -= 4;
            }
            if (count >= 2)
            {
                *(ushort*)dst = *(ushort*)src;
                dst += 2;
                src += 2;
                count -= 2;
            }
            if (count >= 1)
            {
                *dst = *src;
            }
        }

        public void SyncRTCTransform()
        {
            if (m_enableSync == false) return;

            #region unsage�R�[�h���g�p�����p�P�b�g�̐���

            // transform
            // (3 + 4 + 3) * 4 = 40 byte

            // id
            // 1 + (...)

            float[] rtcTransform = new float[10];

            rtcTransform[0] = this.transform.position.x;
            rtcTransform[1] = this.transform.position.y;
            rtcTransform[2] = this.transform.position.z;

            rtcTransform[3] = this.transform.rotation.x;
            rtcTransform[4] = this.transform.rotation.y;
            rtcTransform[5] = this.transform.rotation.z;
            rtcTransform[6] = this.transform.rotation.w;

            rtcTransform[7] = this.transform.localScale.x;
            rtcTransform[8] = this.transform.localScale.y;
            rtcTransform[9] = this.transform.localScale.z;

            byte[] id = System.Text.Encoding.UTF8.GetBytes(this.gameObject.name);
            byte[] packet = new byte[1 + name.Length + rtcTransform.Length * sizeof(float)];

            packet[0] = (byte)name.Length;

            int offset = name.Length;
            int nOffset = 1 + offset;
            int dataLen = rtcTransform.Length * sizeof(float);

            unsafe
            {
                // id
                fixed (byte* iniP = packet, iniD = id)
                {
                    //for (byte* pt = iniP + 1, pd = iniD; pt < iniP + nOffset; pt++, pd++) *pt = *pd;
                    LongCopy(iniD, iniP + 1, nOffset);
                }

                // transform
                fixed (byte* iniP = packet)
                fixed (float* iniD = &(rtcTransform[0]))
                {
                    //for (byte* pt = iniP + nOffset, pd = (byte*)iniD; pt < iniP + nOffset + dataLen; pt++, pd++) *pt = *pd;
                    LongCopy((byte*)iniD, iniP + nOffset, dataLen);
                }
            }

            #endregion unsage�R�[�h���g�p�����p�P�b�g�̐���

            TLabSyncClient.Instalce.SendRTCMessage(packet);

            m_isSyncFromOutside = false;
        }

        public void SyncTransform()
        {
            if (m_enableSync == false) return;

            #region StringBuilder�Ńp�P�b�g�̐����̍�����

            builder.Clear();

            builder.Append("{");
            builder.Append(TLabSyncClientConst.ROLE);
            builder.Append(((int)WebRole.GUEST).ToString());
            builder.Append(TLabSyncClientConst.COMMA);

            builder.Append(TLabSyncClientConst.ACTION);
            builder.Append(((int)WebAction.SYNCTRANSFORM).ToString());
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
            #endregion

            #region ��肽���p�P�b�g
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
            #endregion

            TLabSyncClient.Instalce.SendWsMessage(json);

            m_isSyncFromOutside = false;
        }

        public void ClearTransform()
        {
            if (m_enableSync == false) return;

            TLabSyncClient.Instalce.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.CLEARTRANSFORM,
                seatIndex: TLabSyncClient.Instalce.SeatIndex,
                transform: new WebObjectInfo { id = this.gameObject.name });
        }

        private void RbCompletion()
        {
            // Rigidbody�̓����Ƀ��O������Ƃ��C���b�Z�[�W���͂��Ȃ��Ԃ�Gravity��L���ɂ��ă��[�J���̊��ŕ������Z���s���D
            // �������C�N�����I�u�W�F�N�g��͂�ł��邱�Ƃ��������Ă���Ƃ��́C�����̕������Z�͍s��Ȃ��D

            // Windows 12's Core i 9: 400 -----> Size: 10
            // Oculsu Quest 2: 72 -----> Size: 10 * 72 / 400 = 1.8 ~= 2

#if UNITY_EDITOR
            if (IsUseGravity == true && m_didnotReachCount > 10)
            {
#else
            if (IsUseGravity == true && m_didnotReachCount > 2)
            {
#endif
                if(m_grabbed != -1 &&
                   m_grabbed != -2 &&
                   m_grabbed != TLabSyncClient.Instalce.SeatIndex)
                    if (m_gravityState == false)
                        SetGravity(true);
            }
        }
#endregion SyncTransform

        #region Divide
        public void OnDevideButtonClick()
        {
            Devide();
        }

        public void DivideFromOutside(bool active)
        {
            base.Devide(active);
        }

        public override int Devide()
        {
            int result = base.Devide();

            if (result < 0) return -1;

            bool active = result == 0 ? true : false;

            // ����/������؂�ւ����̂ŁC�N�����̃I�u�W�F�N�g��͂�ł��Ȃ���Ԃɂ���
            TLabSyncGrabbable[] grabbables = GetComponentsInTargets<TLabSyncGrabbable>(DivideTargets);
            foreach (TLabSyncGrabbable grabbable in grabbables) grabbable.ForceRelease();

            TLabSyncClient.Instalce.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.DIVIDEGRABBER,
                active: active,
                transform: new WebObjectInfo { id = this.gameObject.name });

            return result;
        }

        public override void SetInitialChildTransform()
        {
            base.SetInitialChildTransform();

            if (m_enableDivide == false)
                return;

            TLabSyncGrabbable[] grabbables = GetComponentsInTargets<TLabSyncGrabbable>(DivideTargets);
            foreach (TLabSyncGrabbable grabbable in grabbables) grabbable.SyncTransform();
        }
        #endregion Divide

        public void ShutdownGrabber(bool deleteCache)
        {
            if (m_shutdown == true || SocketIsOpen == false)
                return;

            // ���̃I�u�W�F�N�g�����b�N���Ă���̂��������������������
            if (TLabSyncClient.Instalce.SeatIndex == m_grabbed &&
                m_grabbed != -1 &&
                m_grabbed != -2) GrabbLock(false);

            if (deleteCache == true)
                ClearTransform();

            m_shutdown = true;
            m_enableSync = false;
        }

        protected override void Start()
        {
            base.Start();

            // Disable gravity untile graivity allocated from sync server
            SetGravity(false);

            StartCoroutine(RegistRbObj());

            TLabSyncClient.Instalce.AddSyncGrabbable(this.gameObject.name, this);
        }

        protected override void Update()
        {
            RbCompletion();
            CashRbVelocity();

            if (m_mainParent != null)
            {
                if (m_subParent != null && m_scaling)
                    UpdateScale();
                else
                {
                    m_scaleInitialDistance = -1.0f;
                    UpdatePosition();
                }

                SyncRTCTransform();
            }
            else
            {
                m_scaleInitialDistance = -1.0f;

                if (EnableAutoSync == true)
                    SyncRTCTransform();
            }

            m_didnotReachCount++;
        }

        private void OnDestroy()
        {
            ShutdownGrabber(false);
        }

        private void OnApplicationQuit()
        {
            ShutdownGrabber(false);
        }
    }

#if UNITY_EDITOR
    [CustomEditor(typeof(TLabSyncGrabbable))]
    [CanEditMultipleObjects]

    public class TLabSyncGrabbableEditor : Editor
    {
        private void InitializeForRotateble(TLabSyncGrabbable grabbable, TLabVRRotatable rotatable)
        {
            grabbable.InitializeRotatable();
            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(rotatable);
        }

        private void InitializeForDivibable(TLabSyncGrabbable grabbable, bool isRoot)
        {
            // Disable Rigidbody.useGrabity
            grabbable.m_enableSync = true;
            grabbable.m_autoSync = false;
            grabbable.m_locked = false;
            grabbable.UseRigidbody(false, false);

            grabbable.gameObject.layer = LayerMask.NameToLayer("TLabGrabbable");

            var meshFilter = grabbable.gameObject.RequireComponent<MeshFilter>();
            var rotatable = grabbable.gameObject.RequireComponent<TLabSyncRotatable>();
            var meshCollider = grabbable.gameObject.RequireComponent<MeshCollider>();
            meshCollider.enabled = isRoot;

            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(rotatable);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            serializedObject.Update();

            TLabSyncGrabbable grabbable = target as TLabSyncGrabbable;
            TLabVRRotatable rotatable = grabbable.gameObject.GetComponent<TLabVRRotatable>();

            if (rotatable != null && GUILayout.Button("Initialize for Rotatable"))
                InitializeForRotateble(grabbable, rotatable);

            if (grabbable.EnableDivide == true && GUILayout.Button("Initialize for Devibable"))
            {
                InitializeForDivibable(grabbable, true);

                foreach (GameObject divideTarget in grabbable.DivideTargets)
                {
                    var grabbableChild = divideTarget.gameObject.RequireComponent<TLabSyncGrabbable>();
                    InitializeForDivibable(grabbableChild, false);
                }
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}