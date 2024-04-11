using UnityEngine;
using UnityEditor;

namespace TLab.VRClassroom.Editor
{
    [CustomEditor(typeof(Entrance))]
    [CanEditMultipleObjects]

    public class EntranceEditor : UnityEditor.Editor
    {
        private Entrance m_instance;

        private void OnEnable()
        {
            m_instance = target as Entrance;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Command Parse Test"))
            {
                m_instance.PasswordTest("192.168.3.11 -p 1234");
            }
        }
    }
}
