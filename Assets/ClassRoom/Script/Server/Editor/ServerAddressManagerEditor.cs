using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace TLab.VRClassroom.Editor
{
    [CustomEditor(typeof(ServerAddressManager))]
    public class ServerAddressManagerEditor : UnityEditor.Editor
    {
        private ServerAddressManager instance;

        private void OnEnable()
        {
            instance = target as ServerAddressManager;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Set Server Addr"))
            {
                instance.SetServerAddr();
            }
        }
    }
}
