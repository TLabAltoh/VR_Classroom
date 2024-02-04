using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TLab.XR.Network
{
    public class NetworkedObject : MonoBehaviour
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        protected static string REGISTRY = "[registry] ";

        protected static void Register(string id, NetworkedObject networkedObject)
        {
            if (!m_registry.ContainsKey(id))
            {
                m_registry[id] = networkedObject;
            }
        }

        protected static void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
            {
                m_registry.Remove(id);
            }
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

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static void ClearObject(GameObject go)
        {
            if (go.GetComponent<NetworkedObject>() != null)
            {
                Destroy(go);
            }
        }

        public static void ClearObject(string id)
        {
            var go = GetById(id).gameObject;
            if (go != null)
            {
                ClearObject(go);
            }
        }

        public static NetworkedObject GetById(string id) => m_registry[id] as NetworkedObject;

        #endregion REGISTRY

        [Header("Sync Setting")]

        [SerializeField] protected bool m_enableSync = false;

        [SerializeField] protected string m_hash = "";

        protected string m_id = "";

        protected bool m_shutdown = false;

        protected bool m_syncFromOutside = false;

        public string id => m_id;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        public bool enableSync { get => m_enableSync; set => m_enableSync = value; }

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

        public virtual void OnRTCMessage(string dst, string src, byte[] bytes)
        {

        }

        public virtual void Shutdown(bool deleteCache)
        {
            if (m_shutdown || !socketIsOpen)
            {
                return;
            }

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

        protected virtual void OnValidate(){ }
#endif

        protected virtual void Start()
        {
            m_id = gameObject.name + m_hash;

            Register(m_id, this);
        }

        protected virtual void Update() { }

        protected virtual void OnDestroy() => Shutdown(false);

        protected virtual void OnApplicationQuit() => Shutdown(false);
    }
}
