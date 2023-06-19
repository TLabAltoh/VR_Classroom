using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[CustomEditor(typeof(TLabShelfSyncManager))]
[CanEditMultipleObjects]
public class TLabShelfSyncManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        TLabShelfSyncManager manager = target as TLabShelfSyncManager;

        if (GUILayout.Button("Initialize Shelf Obj"))
        {
            foreach (TLabShelfObjInfo shelfInfo in manager.m_shelfObjInfos)
            {
                TLabSyncGrabbable grabbable = shelfInfo.obj.GetComponent<TLabSyncGrabbable>();
                if (grabbable == null) grabbable = shelfInfo.obj.AddComponent<TLabSyncGrabbable>();

                // Rigidbody‚ÌUseGravity‚ð–³Œø‰»‚·‚é

                grabbable.m_enableSync = true;
                grabbable.m_autoSync = false;
                grabbable.m_locked = false;

                grabbable.UseRigidbody(false, false);

                TLabSyncRotatable rotatable = grabbable.gameObject.GetComponent<TLabSyncRotatable>();
                if (rotatable == null) grabbable.gameObject.AddComponent<TLabSyncRotatable>();

                EditorUtility.SetDirty(grabbable);
                EditorUtility.SetDirty(rotatable);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif