using System.Collections.Generic;
using System.Collections;
using UnityEngine;

namespace TLab.XR.Interact
{
    [RequireComponent(typeof(ExclusiveController))]
    public class GrabbableHandle : Handle
    {
        private ExclusiveController m_controller;

        public ExclusiveController controller => m_controller;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

#region REGISTRY

        private static Hashtable m_registry = new Hashtable();

        public static new Hashtable registry => m_registry;

        protected static void Register(string id, GrabbableHandle grabbableHandle)
        {
            if (!m_registry.ContainsKey(id))
            {
                m_registry[id] = grabbableHandle;

                Debug.Log(REGISTRY + "grabbableHandle registered in the registry: " + id);
            }
        }

        protected static void UnRegister(string id)
        {
            if (m_registry.ContainsKey(id))
            {
                m_registry.Remove(id);

                Debug.Log(REGISTRY + "deregistered grabbableHandle from the registry.: " + id);
            }
        }

        public static void ClearRegistry()
        {
            var gameobjects = new List<GameObject>();

            foreach (DictionaryEntry entry in m_registry)
            {
                var grabbable = entry.Value as GrabbableHandle;
                gameobjects.Add(grabbable.gameObject);
            }

            for (int i = 0; i < gameobjects.Count; i++)
            {
                Destroy(gameobjects[i]);
            }

            m_registry.Clear();
        }

        public static GrabbableHandle GetById(string id) => m_registry[id] as GrabbableHandle;

#endregion REGISTRY

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

        public override void Selected(TLabXRHand hand)
        {
            switch (m_controller.OnGrabbed(hand))
            {
                case ExclusiveController.HandType.MAIN_HAND:

                    IgnoreCollision(hand, true);

                    break;
                case ExclusiveController.HandType.SUB_HAND:

                    IgnoreCollision(hand, true);

                    break;
                case ExclusiveController.HandType.NONE:
                    break;
            }

            base.Selected(hand);
        }

        public override void UnSelected(TLabXRHand hand)
        {
            switch (m_controller.GetHandType(hand))
            {
                case ExclusiveController.HandType.MAIN_HAND:

                    IgnoreCollision(hand, false);

                    m_controller.OnRelease(hand);

                    break;
                case ExclusiveController.HandType.SUB_HAND:

                    IgnoreCollision(hand, false);

                    m_controller.OnRelease(hand);

                    break;
                case ExclusiveController.HandType.NONE:
                    break;
            }

            base.UnSelected(hand);
        }

        public override void WhileSelected(TLabXRHand hand)
        {
            base.WhileSelected(hand);
        }

        protected override void Start()
        {
            base.Start();

            m_controller = GetComponent<ExclusiveController>();
        }
    }
}
