using UnityEngine;
using UnityEditor;

namespace TLab.XR.Network.Editor
{
    [CustomEditor(typeof(NetworkedObject))]
    public class NetworkedObjectEditor : UnityEditor.Editor
    {
        private NetworkedObject instance;

        private void OnEnable()
        {
            instance = target as NetworkedObject;
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
