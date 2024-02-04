using UnityEngine;
using UnityEditor;

namespace TLab.VRClassroom.Editor
{
    [CustomEditor(typeof(SyncShelfManager))]
    public class SyncShelfManagerEditor : UnityEditor.Editor
    {
        private SyncShelfManager m_instance;

        private int m_objIndex = 0;

        private GUIContent[] m_options = new[]
        {
            new GUIContent("0"),
            new GUIContent("1"),
            new GUIContent("2"),
        };

        private void OnEnable()
        {
            m_instance = target as SyncShelfManager;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (Application.isPlaying)
            {
                EditorGUILayout.Space();

                m_objIndex = EditorGUILayout.Popup(
                    label: new GUIContent("Obj Index"),
                    selectedIndex: m_objIndex, m_options);

                EditorGUILayout.Space();

                using (var horizontalScope = new GUILayout.HorizontalScope("box"))
                {
                    if (GUILayout.Button("Change Index"))
                    {
                        m_instance.OnDropDownChanged(m_objIndex);
                    }

                    if (GUILayout.Button("Takeout"))
                    {
                        m_instance.TakeOut();
                    }

                    if (GUILayout.Button("Putaway"))
                    {
                        m_instance.PutAway();
                    }

                    if (GUILayout.Button("Shere"))
                    {
                        m_instance.Share();
                    }

                    if (GUILayout.Button("Collect"))
                    {
                        m_instance.Collect();
                    }
                }
            }
        }
    }
}
