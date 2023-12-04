using UnityEngine;
using UnityEditor;

namespace TLab.XR.Network
{
    [CustomEditor(typeof(NetworkedObject))]
    public class NetworkedObjectEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var networkedObject = target as NetworkedObject;

            if (GUILayout.Button("Create Hash ID"))
            {
                networkedObject.CreateHashID();
            }
        }
    }
}
