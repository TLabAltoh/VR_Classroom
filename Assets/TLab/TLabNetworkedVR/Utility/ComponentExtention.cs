using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.VRGrabber.Utility
{
    public static class ComponentExtention
    {
        public static T[] GetComponentsInTargets<T>(GameObject[] targets) where T : Component
        {
            List<T> componentList = new List<T>();
            foreach (GameObject target in targets)
            {
                T component = target.GetComponent<T>();
                if (component != null) componentList.Add(component);
            }

            return componentList.ToArray();
        }

        public static T RequireComponent<T>(this GameObject self) where T : Component
        {
            var result = self.GetComponent<T>();

            if (result == null) result = self.AddComponent<T>();

            return result;
        }

        public static void RemoveComponent<T>(this GameObject self) where T : Component
        {
            var result = self.GetComponent<T>();

            if (result != null) Object.Destroy(result);
        }
    }
}
