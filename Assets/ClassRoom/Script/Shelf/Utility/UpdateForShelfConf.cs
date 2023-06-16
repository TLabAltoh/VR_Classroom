using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class UpdateForShelfConf : MonoBehaviour
{

}

#if UNITY_EDITOR
[CustomEditor(typeof(UpdateForShelfConf))]
[CanEditMultipleObjects]
public class UpdateForShelfConfEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        UpdateForShelfConf shelfConf = target as UpdateForShelfConf;

        if (GUILayout.Button("Update for shelf conf"))
        {
            foreach (Transform goTransform in shelfConf.gameObject.GetComponentsInChildren<Transform>())
            {
                TLabSyncGrabbable grabbable = goTransform.gameObject.GetComponent<TLabSyncGrabbable>();
                if (grabbable == null)
                    grabbable = goTransform.gameObject.AddComponent<TLabSyncGrabbable>();

                grabbable.gameObject.layer = LayerMask.NameToLayer("TLabGrabbable");
            }

            foreach (TLabSyncGrabbable grabbable in shelfConf.gameObject.GetComponentsInChildren<TLabSyncGrabbable>())
            {
                if(grabbable.gameObject != shelfConf.gameObject)
                {
                    MeshCollider meshCollider = grabbable.gameObject.GetComponent<MeshCollider>();
                    if (meshCollider == null)
                        meshCollider = grabbable.gameObject.AddComponent<MeshCollider>();
                    meshCollider.enabled = false;
                }

                grabbable.m_enableSync = true;
                grabbable.m_autoSync = false;
                grabbable.m_locked = false;

                grabbable.UseRigidbody(false, false);

                EditorUtility.SetDirty(grabbable);

                TLabSyncRotatable rotatable = grabbable.gameObject.GetComponent<TLabSyncRotatable>();
                if (rotatable == null)
                    rotatable = grabbable.gameObject.AddComponent<TLabSyncRotatable>();

                EditorUtility.SetDirty(rotatable);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif