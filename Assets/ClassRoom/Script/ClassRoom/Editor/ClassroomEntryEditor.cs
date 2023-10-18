using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
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
                string hash = TLabSecurity.GetHashString(classroomEntry.Password);
                classroomEntry.PassHash = hash;
                classroomEntry.Password = "";

                EditorUtility.SetDirty(classroomEntry);
            }
        }
    }
}
