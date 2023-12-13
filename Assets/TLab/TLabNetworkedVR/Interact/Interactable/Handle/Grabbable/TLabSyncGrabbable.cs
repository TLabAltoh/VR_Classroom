using System.Collections;
using System.Text;
using UnityEngine;
using TLab.XR.Network;
using static TLab.XR.ComponentExtention;

namespace TLab.XR.VRGrabber
{
    public class TLabSyncGrabbable : TLabVRGrabbable
    {
        [Header("Sync Setting")]

        [Tooltip("これが有効化されていないとオブジェクトは同期されない")]
        [SerializeField] public bool m_enableSync = false;

        [Tooltip("有効化すると毎フレーム自動でオブジェクトを同期する")]
        [SerializeField] public bool m_autoSync = false;

        [Tooltip("有効化すると誰からもこのオブジェクトを掴めなくなる")]
        [SerializeField] public bool m_locked = false;

        [Tooltip("Rigidbodyの更新処理を任されているか (Debug)")]
        [SerializeField] private bool m_rbAllocated = false;

        private int m_grabbed = -1;

        private bool m_isSyncFromOutside = false;
        private int m_didnotReachCount = 0;

        private bool m_shutdown = false;

        // https://www.fenet.jp/dotnet/column/language/4836/
        // A fast approach to string processing

        private StringBuilder builder = new StringBuilder();

        //
        private const string THIS_NAME = "[tlabsyncgrabbable] ";

        public bool IsEnableGravity { get => (m_rb == null) ? false : m_rb.useGravity; }

        public bool IsUseGravity { get => m_useGravity; }

        public int GrabbedIndex { get => m_grabbed; }

        public bool IsGrabbLocked { get => m_grabbed != -1; }

        public bool RbAllocated { get => m_rbAllocated; }

        private bool EnableAutoSync { get => m_enableSync && !IsGrabbLocked && (m_autoSync || m_rbAllocated && IsEnableGravity); }

        public bool IsSyncFromOutside { get => m_isSyncFromOutside; }

        private bool SocketIsOpen { get => (SyncClient.Instance != null && SyncClient.Instance.socketIsOpen && SyncClient.Instance.seatIndex != -1); }

        public void SyncFromOutside(WebObjectInfo transform)
        {
            WebVector3 position = transform.position;
            WebVector3 scale = transform.scale;
            WebVector4 rotation = transform.rotation;

            this.transform.localScale = new Vector3(scale.x, scale.y, scale.z);

            if (m_useRigidbody)
            {
                // Rigidbodyを無効化し，同期される側でも速度を正しく計算できるようにする．
                if (!m_rbAllocated && m_gravityState)
                {
                    SetGravity(false);
                }

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
            if (!m_useGravity)
            {
                yield break;
            }

            // Wait for connection is opened
            while (!SocketIsOpen)
            {
                yield return null;
            }

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.REGISTRBOBJ,
                transform: new WebObjectInfo { id = this.gameObject.name });

            Debug.Log(THIS_NAME + "Send Rb Obj");
        }

        public void AllocateGravity(bool active)
        {
            m_rbAllocated = active;

            bool allocated = m_grabbed == -1 && active;

            SetGravity(allocated ? true : false);

            Debug.Log(THIS_NAME + "rb allocated:" + allocated + " - " + this.gameObject.name);
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

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.FORCERELEASE,
                transform: new WebObjectInfo { id = this.gameObject.name });

            Debug.Log(THIS_NAME + "force release");
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

                if (m_rbAllocated)
                {
                    SetGravity(false);
                }
            }
            else
            {
                m_grabbed = -1;
                if (m_rbAllocated)
                {
                    SetGravity(true);
                }
            }
        }

        public void GrabbLock(bool active)
        {
            m_grabbed = active ? SyncClient.Instance.seatIndex : -1;

            if (m_rbAllocated)
            {
                SetGravity(!active);
            }

            SyncTransform();

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.GRABBLOCK,
                seatIndex: active ? SyncClient.Instance.seatIndex : -1,
                transform: new WebObjectInfo { id = this.gameObject.name });

            Debug.Log(THIS_NAME + "grabb lock");
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

            if (m_rbAllocated)
            {
                SetGravity(!active);
            }

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.GRABBLOCK,
                seatIndex: active ? -2 : -1,
                transform: new WebObjectInfo { id = this.gameObject.name });

            Debug.Log(THIS_NAME + "simple lock");
        }

        protected override void RbGripSwitch(bool grip)
        {
            GrabbLock(grip);
        }

        public override bool AddParent(GameObject parent)
        {
            if (m_locked || m_grabbed != -1 && m_grabbed != SyncClient.Instance.seatIndex)
            {
                return false;
            }

            return base.AddParent(parent);
        }


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
            if (!m_enableSync)
            {
                return;
            }

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

            SyncClient.Instance.SendRTCMessage(packet);

            m_isSyncFromOutside = false;
        }

        public void SyncTransform()
        {
            if (!m_enableSync)
            {
                return;
            }

            builder.Clear();

            builder.Append("{");
            builder.Append(SyncClientConst.ROLE);
            builder.Append(((int)WebRole.GUEST).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.ACTION);
            builder.Append(((int)WebAction.SYNCTRANSFORM).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.TRANSFORM);
            builder.Append("{");
            builder.Append(SyncClientConst.TRANSFORM_ID);
            builder.Append("\"");
            builder.Append(this.gameObject.name);
            builder.Append("\"");
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.RIGIDBODY);
            builder.Append((m_useRigidbody ? "true" : "false"));
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.GRAVITY);
            builder.Append((m_useGravity ? "true" : "false"));
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.POSITION);
            builder.Append("{");
            builder.Append(SyncClientConst.X);
            builder.Append((this.transform.position.x).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.Y);
            builder.Append((this.transform.position.y).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.Z);
            builder.Append((this.transform.position.z).ToString());
            builder.Append("}");
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.ROTATION);
            builder.Append("{");
            builder.Append(SyncClientConst.X);
            builder.Append((this.transform.rotation.x).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.Y);
            builder.Append((this.transform.rotation.y).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.Z);
            builder.Append((this.transform.rotation.z).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.W);
            builder.Append((this.transform.rotation.w).ToString());
            builder.Append("}");
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.SCALE);
            builder.Append("{");
            builder.Append(SyncClientConst.X);
            builder.Append((this.transform.localScale.x).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.Y);
            builder.Append((this.transform.localScale.y).ToString());
            builder.Append(SyncClientConst.COMMA);

            builder.Append(SyncClientConst.Z);
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

            SyncClient.Instance.SendWsMessage(json);

            m_isSyncFromOutside = false;
        }

        public void ClearTransform()
        {
            if (!m_enableSync)
            {
                return;
            }

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.CLEARTRANSFORM,
                seatIndex: SyncClient.Instance.seatIndex,
                transform: new WebObjectInfo { id = this.gameObject.name });
        }

        private void RbCompletion()
        {
            // Rigidbodyの同期にラグがあるとき，メッセージが届かない間はGravityを有効にしてローカルの環境で物理演算を行う．
            // ただし，誰かがオブジェクトを掴んでいることが分かっているときは，推測の物理演算は行わない．

            // Windows 12's Core i 9: 400 -----> Size: 10
            // Oculsu Quest 2: 72 -----> Size: 10 * 72 / 400 = 1.8 ~= 2

#if UNITY_EDITOR
            if (IsUseGravity && m_didnotReachCount > 10)
            {
#else
            if (IsUseGravity && m_didnotReachCount > 2)
            {
#endif
                if(m_grabbed == -1 && !m_rbAllocated && !m_gravityState)
                {
                    SetGravity(true);
                }
            }
        }


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

            if (result < 0)
            {
                return -1;
            }

            bool active = result == 0 ? true : false;

            // 結合/分割を切り替えたので，誰もこのオブジェクトを掴んでいない状態にする
            TLabSyncGrabbable[] grabbables = GetComponentsInTargets<TLabSyncGrabbable>(DivideTargets);
            foreach (TLabSyncGrabbable grabbable in grabbables)
            {
                grabbable.ForceRelease();
            }

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.DIVIDEGRABBER,
                active: active,
                transform: new WebObjectInfo { id = this.gameObject.name });

            return result;
        }

        public override void SetInitialChildTransform()
        {
            base.SetInitialChildTransform();

            if (!m_enableDivide)
            {
                return;
            }

            TLabSyncGrabbable[] grabbables = GetComponentsInTargets<TLabSyncGrabbable>(DivideTargets);
            foreach (TLabSyncGrabbable grabbable in grabbables)
            {
                grabbable.SyncRTCTransform();
                grabbable.SyncTransform();
            }
        }


        public void ShutdownGrabber(bool deleteCache)
        {
            if (m_shutdown || !SocketIsOpen)
            {
                return;
            }

            // このオブジェクトをロックしているのが自分だったら解除する
            if (SyncClient.Instance.seatIndex == m_grabbed && m_grabbed != -1 && m_grabbed != -2)
            {
                GrabbLock(false);
            }

            if (!deleteCache)
            {
                ClearTransform();
            }

            m_shutdown = true;
            m_enableSync = false;
        }

        protected override void Start()
        {
            m_rbAllocated = false;

            base.Start();

            // Disable gravity untile graivity allocated from sync server
            SetGravity(false);

            StartCoroutine(RegistRbObj());

            //SyncClient.Instance.AddSyncGrabbable(this.gameObject.name, this);
        }

        protected override void Update()
        {
            RbCompletion();
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

                SyncRTCTransform();
            }
            else
            {
                m_scaleInitialDistance = -1.0f;

                if (EnableAutoSync)
                {
                    SyncRTCTransform();
                }
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
}