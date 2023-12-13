using UnityEngine;
using UnityEditor;
using TLab.Security;

namespace TLab.VRClassroom.Editor
{
    [CustomEditor(typeof(ClassroomEntry))]
    [CanEditMultipleObjects]

    public class ClassroomEntryEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            ClassroomEntry classroomEntry = target as ClassroomEntry;

            if (GUILayout.Button("Regist Password"))
            {
                string hash = TLabSecurity.GetHashString(classroomEntry.password);
                classroomEntry.passwordHash = hash;
                classroomEntry.password = "";

                EditorUtility.SetDirty(classroomEntry);
            }

            if (GUILayout.Button("Password Test"))
            {
                classroomEntry.PasswordTest("192.168.3.11 -p 1234");
            }
        }
    }
}
