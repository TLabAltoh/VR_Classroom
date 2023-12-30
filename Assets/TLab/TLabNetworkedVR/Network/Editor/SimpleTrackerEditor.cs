using UnityEngine;
using UnityEditor;

namespace TLab.XR.Network.Editor
{
    [CustomEditor(typeof(SimpleTracker))]
    public class SimpleTrackerEditor : UnityEditor.Editor
    {
        private SimpleTracker instance;

        private void OnEnable()
        {
            instance = target as SimpleTracker;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Create Hash ID"))
            {
                instance.CreateHashID();
            }
        }
    }
}
