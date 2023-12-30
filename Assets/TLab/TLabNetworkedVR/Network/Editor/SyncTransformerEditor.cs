using UnityEngine;
using UnityEditor;

namespace TLab.XR.Network.Editor
{
    [CustomEditor(typeof(SyncTransformer))]
    public class SyncTransformerEditor : UnityEditor.Editor
    {
        private SyncTransformer instance;

        private void OnEnable()
        {
            instance = target as SyncTransformer;
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
