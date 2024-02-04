using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;

namespace TLab.XR.Network
{
    public class SyncTransformer : NetworkedObject
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="syncTransformer"></param>
        protected static void Register(string id, SyncTransformer syncTransformer)
        {
            if (!m_registry.ContainsKey(id))
            {
                m_registry[id] = syncTransformer;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        protected static new void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
            {
                m_registry.Remove(id);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public static new void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var syncTransformer = entry.Value as SyncTransformer;
                gameobjects.Add(syncTransformer.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="go"></param>
        public static new void ClearObject(GameObject go)
        {
            if (go.GetComponent<SyncTransformer>() != null)
            {
                Destroy(go);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public static new void ClearObject(string id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
            {
                ClearObject(go);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static new SyncTransformer GetById(string id) => m_registry[id] as SyncTransformer;

        #endregion REGISTRY

        [Header("Rigidbody settings")]

        [SerializeField] protected bool m_useRigidbody = false;

        [SerializeField] protected bool m_useGravity = false;

#if UNITY_EDITOR
        [SerializeField]
#endif
        protected bool m_rbAllocated = false;

        protected Rigidbody m_rb;

        protected bool m_gravityState = false;

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

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        /// <summary>
        /// 
        /// </summary>
        public Rigidbody rb => m_rb;

        /// <summary>
        /// 
        /// </summary>
        public bool enableGravity => (m_rb == null) ? false : m_rb.useGravity;

        /// <summary>
        /// 
        /// </summary>
        public bool useRigidbody => m_useRigidbody;

        /// <summary>
        /// 
        /// </summary>
        public bool useGravity => m_useGravity;

        /// <summary>
        /// 
        /// </summary>
        public bool rbAllocated => m_rbAllocated;

#if UNITY_EDITOR
        /// <summary>
        /// 
        /// </summary>
        /// <param name="rigidbody"></param>
        /// <param name="gravity"></param>
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
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

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        private IEnumerator RegistRigidbodyObject()
        {
            if (!m_useGravity)  // if useGravity is false, doesn't regist this object to server
            {
                yield break;
            }

            while (!socketIsOpen)   // Wait for connection is opened
            {
                yield return null;
            }

            SyncClient.Instance.SendWsMessage(
                action: WebAction.REGIST_RB_OBJ,
                transform: new WebObjectInfo { id = m_id });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="active"></param>
        public virtual void SetGravity(bool active)
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="srcTransform"></param>
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
                {
                    SetGravity(false);
                }

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="src"></param>
        /// <param name="bytes"></param>
        public override void OnRTCMessage(string dst, string src, byte[] bytes)
        {
            float[] rtcTransform = new float[10];

            unsafe
            {
                fixed (byte* iniP = bytes)  // transform
                fixed (float* iniD = &(rtcTransform[0]))
                {
                    LongCopy(iniP, (byte*)iniD, bytes.Length);
                }
            }

            var webTransform = new WebObjectInfo
            {
                position = new WebVector3 { x = rtcTransform[0], y = rtcTransform[1], z = rtcTransform[2] },
                rotation = new WebVector4 { x = rtcTransform[3], y = rtcTransform[4], z = rtcTransform[5], w = rtcTransform[6] },
                scale = new WebVector3 { x = rtcTransform[7], y = rtcTransform[8], z = rtcTransform[9] }
            };

            SyncFromOutside(webTransform);
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void SyncRTCTransform()
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

            int nameBytesLen = m_id.Length;
            int subBytesStart = 1 + nameBytesLen;
            int subBytesLen = rtcTransform.Length * sizeof(float);

            unsafe
            {
                fixed (byte* iniP = packet, iniD = id)  // id
                {
                    LongCopy(iniD, iniP + 1, nameBytesLen);
                }

                fixed (byte* iniP = packet) // transform
                fixed (float* iniD = &(rtcTransform[0]))
                {
                    LongCopy((byte*)iniD, iniP + 1 + nameBytesLen, subBytesLen);
                }
            }

            SyncClient.Instance.SendRTCMessage(packet);

            m_syncFromOutside = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void SyncTransform()
        {
            if (!m_enableSync)
            {
                return;
            }

            TLabSyncJson obj = new TLabSyncJson
            {
                action = (int)WebAction.SYNC_TRANSFORM,

                transform = new WebObjectInfo
                {
                    id = m_id,

                    rigidbody = m_useRigidbody,
                    gravity = m_useGravity,

                    position = new WebVector3
                    {
                        x = transform.position.x,
                        y = transform.position.y,
                        z = transform.position.z
                    },
                    rotation = new WebVector4
                    {
                        x = transform.rotation.x,
                        y = transform.rotation.y,
                        z = transform.rotation.z,
                        w = transform.rotation.w,
                    },
                    scale = new WebVector3
                    {
                        x = transform.localScale.x,
                        y = transform.localScale.y,
                        z = transform.localScale.z
                    }
                }
            };

            SyncClient.Instance.SendWsMessage(obj);

            m_syncFromOutside = false;
        }

        /// <summary>
        /// 
        /// </summary>
        public virtual void ClearTransform()
        {
            if (!m_enableSync)
            {
                return;
            }

            SyncClient.Instance.SendWsMessage(
                action: WebAction.CLEAR_TRANSFORM,
                transform: new WebObjectInfo { id = m_id });
        }

        /// <summary>
        /// 
        /// </summary>
        protected virtual void CashRbVelocity()
        {
            if (m_rb != null)
            {
                m_prebVels.Enqueue(m_rb.velocity);
                m_prebArgs.Enqueue(m_rb.angularVelocity);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="deleteCache"></param>
        public override void Shutdown(bool deleteCache)
        {
            if (m_shutdown || !socketIsOpen)
            {
                return;
            }

            if (deleteCache)
            {
                ClearTransform();
            }

            UnRegister(m_id);

            base.Shutdown(deleteCache);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Start()
        {
            base.Start();

            m_rbAllocated = false;

            if (m_useRigidbody)
            {
                m_rb = this.gameObject.RequireComponent<Rigidbody>();
                m_prebVels.Enqueue(m_rb.velocity);
                m_prebArgs.Enqueue(m_rb.angularVelocity);

                SetGravity(m_useGravity);
            }

            StartCoroutine(RegistRigidbodyObject());

            Register(m_id, this);
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void Update()
        {
            base.Update();

            CashRbVelocity();

            m_didnotReachCount++;
        }

        /// <summary>
        /// 
        /// </summary>
        protected override void OnDestroy() => Shutdown(false);

        /// <summary>
        /// 
        /// </summary>
        protected override void OnApplicationQuit() => Shutdown(false);
    }
}
