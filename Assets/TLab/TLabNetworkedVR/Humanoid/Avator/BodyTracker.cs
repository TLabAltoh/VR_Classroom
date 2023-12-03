using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TLab.XR.Network;

namespace TLab.XR.Humanoid
{
    public class BodyTracker : NetworkedObject
    {
        [System.Serializable]
        public class TrackTarget
        {
            public AvatorConfig.BodyParts parts;

            public Transform target;
        }

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        #region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static Hashtable registry => m_registry;

        protected static void Register(string id, BodyTracker tracker)
        {
            if (!m_registry.ContainsKey(id))
            {
                m_registry[id] = tracker;

                Debug.Log(REGISTRY + "tracker registered in the registry: " + id);
            }
        }

        protected static new void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
            {
                m_registry.Remove(id);

                Debug.Log(REGISTRY + "deregistered tracker from the registry.: " + id);
            }
        }

        public static new void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var tracker = entry.Value as BodyTracker;
                gameobjects.Add(tracker.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static new BodyTracker GetById(string id) => m_registry[id] as BodyTracker;

        #endregion REGISTRY

        [SerializeField] AvatorConfig.BodyParts m_bodyParts;

        public AvatorConfig.BodyParts bodyParts => m_bodyParts;

        protected override void Start()
        {
            base.Start();

            Register(m_id, this);
        }

        protected override void Update()
        {
            base.Update();

            SyncRTCTransform();
        }

        protected override void OnApplicationQuit()
        {
            UnRegister(m_id);

            base.OnApplicationQuit();
        }

        protected override void OnDestroy()
        {
            UnRegister(m_id);

            base.OnDestroy();
        }
    }
}
