using UnityEngine;
using UnityEditor;

namespace TLab.XR.Network.Editor
{
    [CustomEditor(typeof(SyncClient))]
    public class SyncClientEditor : UnityEditor.Editor
    {
        private SyncClient m_instance;

        private void OnEnable()
        {
            m_instance = target as SyncClient;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"current seat index: {m_instance.seatIndex}", GUILayout.ExpandWidth(false));
                EditorGUILayout.Space();
            }
        }
    }
}
