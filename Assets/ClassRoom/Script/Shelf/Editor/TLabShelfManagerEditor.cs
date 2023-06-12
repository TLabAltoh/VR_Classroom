using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[CustomEditor(typeof(TLabShelfManager))]
[CanEditMultipleObjects]
public class TLabShelfManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        TLabShelfManager manager = target as TLabShelfManager;

        if (GUILayout.Button("Initialize Shelf Obj"))
        {
            foreach (TLabShelfObjInfo shelfInfo in manager.m_shelfObjInfos)
            {
                TLabVRGrabbable grabbable = shelfInfo.obj.GetComponent<TLabVRGrabbable>();
                if (grabbable == null)
                    grabbable = shelfInfo.obj.AddComponent<TLabVRGrabbable>();

                grabbable.UseRigidbody(false, false);

                TLabVRRotatable rotatable = grabbable.gameObject.GetComponent<TLabVRRotatable>();
                if (rotatable == null)
                    grabbable.gameObject.AddComponent<TLabVRRotatable>();

                EditorUtility.SetDirty(grabbable);
                EditorUtility.SetDirty(rotatable);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif