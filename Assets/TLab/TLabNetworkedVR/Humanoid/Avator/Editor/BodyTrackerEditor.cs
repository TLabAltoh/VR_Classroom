using UnityEngine;
using UnityEditor;

namespace TLab.XR.Humanoid
{
    [CustomEditor(typeof(BodyTracker))]
    public class BodyTrackerEditor : UnityEditor.Editor
    {
        private BodyTracker instance;

        private void OnEnable()
        {
            instance = target as BodyTracker;
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
