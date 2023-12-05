using UnityEngine;
using UnityEditor;

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

            if (GUILayout.Button("Regist Server Address"))
            {
                instance.RegistServerAddress();
            }
        }
    }
}
