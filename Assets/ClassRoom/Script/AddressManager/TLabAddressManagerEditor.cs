#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TLabAddressManager))]
[CanEditMultipleObjects]
public class TLabAddressManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        if (GUILayout.Button("Set Server Addr"))
        {
            TLabAddressManager manager = target as TLabAddressManager;
            manager.SetServerAddr();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif