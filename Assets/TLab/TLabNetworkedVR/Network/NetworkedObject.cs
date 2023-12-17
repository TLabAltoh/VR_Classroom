using System.Collections.Generic;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEditor;

namespace TLab.XR.Network
{
    public class NetworkedObject : MonoBehaviour
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        protected static string REGISTRY = "[registry] ";

        protected static void Register(string id, NetworkedObject networkedObject)
        {
            if (!m_registry.ContainsKey(id)) m_registry[id] = networkedObject;
        }

        protected static void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id)) m_registry.Remove(id);
        }

        public static void ClearRegistry()
        {
            // TODO: 一度に多くのオブジェクトを廃棄するときに，どんな負荷が加わるかが予想できない
            // 非同期的に廃棄を行う方法を検討する．

            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var networkedObject = entry.Value as NetworkedObject;
                gameobjects.Add(networkedObject.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++) Destroy(gameobjects[i]);

            m_registry.Clear();
        }

        public static void ClearObject(GameObject go)
        {
            if (go.GetComponent<NetworkedObject>() != null) Destroy(go);
        }

        public static void ClearObject(string id)
        {
            var go = GetById(id).gameObject;

            if (go != null) ClearObject(go);
        }

        public static NetworkedObject GetById(string id) => m_registry[id] as NetworkedObject;

        #endregion REGISTRY

        [Header("Sync Setting")]

        [SerializeField] protected bool m_enableSync = false;

        [SerializeField] protected string m_hash = "";

        protected string m_id = "";

        [Header("Rigidbody settings")]

        [SerializeField] protected bool m_useRigidbody = false;

        [SerializeField] protected bool m_useGravity = false;

#if UNITY_EDITOR
        [SerializeField]
#endif
        protected bool m_rbAllocated = false;

        protected Rigidbody m_rb;

        protected bool m_shutdown = false;

        protected bool m_gravityState = false;

        protected bool m_syncFromOutside = false;

        protected int m_didnotReachCount = 0;

#if UNITY_EDITOR
        // Windows 12's Core i 9: 400 -----> Size: 20
        protected const int CASH_COUNT = 20;
#else
        // Oculsu Quest 2: 72 -----> Size: 20 * 72 / 400 = 3.6 ~= 4
        protected const int CASH_COUNT = 5;
#endif

        protected FixedQueue<Vector3> m_prebVels = new FixedQueue<Vector3>(CASH_COUNT);

        protected FixedQueue<Vector3> m_prebArgs = new FixedQueue<Vector3>(CASH_COUNT);

        // https://www.fenet.jp/dotnet/column/language/4836/
        // A fast approach to string processing

        private StringBuilder m_builder = new StringBuilder();

        public string id => m_id;

        public Rigidbody rb => m_rb;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public bool enableSync { get => m_enableSync; set => m_enableSync = value; }

        public bool enableGravity => (m_rb == null) ? false : m_rb.useGravity;

        public bool useRigidbody => m_useRigidbody;

        public bool useGravity => m_useGravity;

        public bool rbAllocated => m_rbAllocated;

        public bool syncFromOutside => m_syncFromOutside;

        public bool socketIsOpen => (SyncClient.Instance != null && SyncClient.Instance.socketIsOpen && SyncClient.Instance.seatIndex != -1);

        protected static unsafe void LongCopy(byte* src, byte* dst, int count)
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

#if UNITY_EDITOR
        public virtual void UseRigidbody(bool rigidbody, bool gravity)
        {
            if (EditorApplication.isPlaying) return;

            m_useRigidbody = rigidbody;
            m_useGravity = gravity;
        }
#endif

        protected Vector3 GetMaxVector(FixedQueue<Vector3> target)
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

        private IEnumerator RegistRigidbodyObject()
        {
            // if useGravity is false, doesn't regist this object to server
            if (!m_useGravity) yield break;

            // Wait for connection is opened
            while (!socketIsOpen) yield return null;

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.REGISTRBOBJ,
                transform: new WebObjectInfo { id = m_id });
        }

        public virtual void SetGravity(bool active)
        {
            if (m_rb == null || m_useRigidbody == false) return;

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

        public void SyncFromOutside(WebObjectInfo srcTransform)
        {
            WebVector3 position = srcTransform.position;
            WebVector3 scale = srcTransform.scale;
            WebVector4 rotation = srcTransform.rotation;

            transform.localScale = new Vector3(scale.x, scale.y, scale.z);

            if (m_useRigidbody)
            {
                // Rigidbodyを無効化し，同期される側でも速度を正しく計算できるようにする．
                if (!m_rbAllocated && m_gravityState)
                    SetGravity(false);

                m_rb.MovePosition(new Vector3(position.x, position.y, position.z));
                m_rb.MoveRotation(new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w));
            }
            else
            {
                transform.position = new Vector3(position.x, position.y, position.z);
                transform.rotation = new Quaternion(rotation.x, rotation.y, rotation.z, rotation.w);
            }

            m_syncFromOutside = true;
            m_didnotReachCount = 0;
        }

        public void SyncRTCTransform()
        {
            if (!m_enableSync) return;

            // transform
            // (3 + 4 + 3) * 4 = 40 byte

            // id
            // 1 + (...)

            float[] rtcTransform = new float[10];

            rtcTransform[0] = transform.position.x;
            rtcTransform[1] = transform.position.y;
            rtcTransform[2] = transform.position.z;

            rtcTransform[3] = transform.rotation.x;
            rtcTransform[4] = transform.rotation.y;
            rtcTransform[5] = transform.rotation.z;
            rtcTransform[6] = transform.rotation.w;

            rtcTransform[7] = transform.localScale.x;
            rtcTransform[8] = transform.localScale.y;
            rtcTransform[9] = transform.localScale.z;

            byte[] id = System.Text.Encoding.UTF8.GetBytes(m_id);
            byte[] packet = new byte[1 + m_id.Length + rtcTransform.Length * sizeof(float)];

            packet[0] = (byte)m_id.Length;

            int offset = m_id.Length;
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

            m_syncFromOutside = false;
        }

        public void SyncTransform()
        {
            if (!m_enableSync) return;

            m_builder.Clear();

            m_builder.Append("{");
            m_builder.Append(SyncClientConst.ROLE);
            m_builder.Append(((int)WebRole.GUEST).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.ACTION);
            m_builder.Append(((int)WebAction.SYNCTRANSFORM).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.TRANSFORM);
            m_builder.Append("{");
            m_builder.Append(SyncClientConst.TRANSFORM_ID);
            m_builder.Append("\"");
            m_builder.Append(m_id);
            m_builder.Append("\"");
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.RIGIDBODY);
            m_builder.Append((m_useRigidbody ? "true" : "false"));
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.GRAVITY);
            m_builder.Append((m_useGravity ? "true" : "false"));
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.POSITION);
            m_builder.Append("{");
            m_builder.Append(SyncClientConst.X);
            m_builder.Append((transform.position.x).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.Y);
            m_builder.Append((transform.position.y).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.Z);
            m_builder.Append((transform.position.z).ToString());
            m_builder.Append("}");
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.ROTATION);
            m_builder.Append("{");
            m_builder.Append(SyncClientConst.X);
            m_builder.Append((transform.rotation.x).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.Y);
            m_builder.Append((transform.rotation.y).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.Z);
            m_builder.Append((transform.rotation.z).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.W);
            m_builder.Append((transform.rotation.w).ToString());
            m_builder.Append("}");
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.SCALE);
            m_builder.Append("{");
            m_builder.Append(SyncClientConst.X);
            m_builder.Append((transform.localScale.x).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.Y);
            m_builder.Append((transform.localScale.y).ToString());
            m_builder.Append(SyncClientConst.COMMA);

            m_builder.Append(SyncClientConst.Z);
            m_builder.Append((transform.localScale.z).ToString());
            m_builder.Append("}");
            m_builder.Append("}");
            m_builder.Append("}");

            string json = m_builder.ToString();

            //TLabSyncJson obj = new TLabSyncJson
            //{
            //    role = (int)WebRole.GUEST,
            //    action = (int)WebAction.SYNCTRANSFORM,

            //    transform = new WebObjectInfo
            //    {
            //        id = m_id,

            //        rigidbody = m_useRigidbody,
            //        gravity = m_useGravity,

            //        position = new WebVector3
            //        {
            //            x = transform.position.x,
            //            y = transform.position.y,
            //            z = transform.position.z
            //        },
            //        rotation = new WebVector4
            //        {
            //            x = transform.rotation.x,
            //            y = transform.rotation.y,
            //            z = transform.rotation.z,
            //            w = transform.rotation.w,
            //        },
            //        scale = new WebVector3
            //        {
            //            x = transform.localScale.x,
            //            y = transform.localScale.y,
            //            z = transform.localScale.z
            //        }
            //    }
            //};

            //string json = JsonUtility.ToJson(obj);

            SyncClient.Instance.SendWsMessage(json);

            m_syncFromOutside = false;
        }

        public void ClearTransform()
        {
            if (!m_enableSync) return;

            SyncClient.Instance.SendWsMessage(
                role: WebRole.GUEST,
                action: WebAction.CLEARTRANSFORM,
                seatIndex: SyncClient.Instance.seatIndex,
                transform: new WebObjectInfo { id = m_id });
        }

        private void CashRbVelocity()
        {
            if (m_rb != null)
            {
                m_prebVels.Enqueue(m_rb.velocity);
                m_prebArgs.Enqueue(m_rb.angularVelocity);
            }
        }

        public void Shutdown(bool deleteCache)
        {
            if (m_shutdown || !socketIsOpen) return;

            if (deleteCache) ClearTransform();

            m_shutdown = true;
            m_enableSync = false;

            UnRegister(m_id);
        }

#if UNITY_EDITOR
        public virtual void CreateHashID()
        {
            var r = new System.Random();
            var v = r.Next();

            m_hash = v.GetHashCode().ToString();
        }

        protected virtual void OnValidate(){}
#endif

        protected virtual void Start()
        {
            m_rbAllocated = false;

            if (m_useRigidbody)
            {
                m_rb = this.gameObject.RequireComponent<Rigidbody>();
                m_prebVels.Enqueue(m_rb.velocity);
                m_prebArgs.Enqueue(m_rb.angularVelocity);

                SetGravity(m_useGravity);
            }

            StartCoroutine(RegistRigidbodyObject());

            m_id = gameObject.name + m_hash;

            Register(m_id, this);
        }

        protected virtual void Update()
        {
            CashRbVelocity();

            m_didnotReachCount++;
        }

        protected virtual void OnDestroy() => Shutdown(false);

        protected virtual void OnApplicationQuit() => Shutdown(false);
    }
}
