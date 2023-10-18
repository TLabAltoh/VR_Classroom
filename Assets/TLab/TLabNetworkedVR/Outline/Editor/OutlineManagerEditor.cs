using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.XR.VRGrabber.VFX.Editor
{

    [CustomEditor(typeof(OutlineManager))]
    public class TLabOutlineManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            OutlineManager manager = target as OutlineManager;

            if (GUILayout.Button("Process Mesh"))
            {
                if (manager.SelectMeshSavePath())
                {
                    manager.ProcessMesh();
                }

                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Create Outline"))
            {
                if (manager.SelectMaterialSavePath())
                {
                    manager.CreateOutline();
                }

                EditorUtility.SetDirty(manager);
            }
        }
    }
}
