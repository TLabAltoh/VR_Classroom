using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.Network
{
    public class SimpleTracker : NetworkedObject
    {
        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static Hashtable registry => m_registry;

        protected static void Register(string id, SimpleTracker tracker)
        {
            if (!m_registry.ContainsKey(id))
            {
                m_registry[id] = tracker;

                Debug.Log(REGISTRY + "simple tracker registered in the registry: " + id);
            }
        }

        protected static new void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
            {
                m_registry.Remove(id);

                Debug.Log(REGISTRY + "deregistered simple tracker from the registry.: " + id);
            }
        }

        public static new void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var tracker = entry.Value as SimpleTracker;
                gameobjects.Add(tracker.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static new void ClearObject(GameObject go)
        {
            if (go.GetComponent<SimpleTracker>() != null)
            {
                Destroy(go);
            }
        }

        public static new void ClearObject(string id)
        {
            var go = GetById(id).gameObject;

            if (go != null)
            {
                ClearObject(go);
            }
        }

        public static new SimpleTracker GetById(string id) => m_registry[id] as SimpleTracker;

        #endregion REGISTRY

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        private bool m_self = false;

        public bool self { get => m_self; set => m_self = value; }

        public void Shutdown() => UnRegister(m_id);

        protected override void Start()
        {
            m_useGravity = false;

            base.Start();

            Register(m_id, this);
        }

        protected override void Update()
        {
            base.Update();

            if (m_self)
            {
                SyncRTCTransform();
            }
        }

        protected override void OnApplicationQuit()
        {
            Shutdown();

            base.OnApplicationQuit();
        }

        protected override void OnDestroy()
        {
            Shutdown();

            base.OnDestroy();
        }
    }
}
