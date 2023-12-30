using UnityEngine;
using UnityEditor;

namespace TLab.XR.Interact.Editor
{
    [CustomEditor(typeof(DebugInteractor))]
    public class DebugInteractorEditor : UnityEditor.Editor
    {
        private DebugInteractor m_interactor;

        private void OnEnable()
        {
            m_interactor = target as DebugInteractor;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Grab"))
            {
                m_interactor.Grab();
            }

            if (GUILayout.Button("Release"))
            {
                m_interactor.Release();
            }
        }
    }
}
