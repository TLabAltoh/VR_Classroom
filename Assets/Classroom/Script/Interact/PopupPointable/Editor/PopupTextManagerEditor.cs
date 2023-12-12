using UnityEngine;
using UnityEditor;
using TLab.XR.Interact;

namespace TLab.VRClassroom.Editor
{
    [CustomEditor(typeof(PopupTextManager))]
    public class PopupTextManagerEditor : UnityEditor.Editor
    {
        private void OverwritePopupPointable(int index, ref PopupTextManager manager)
        {
            const string NAME = "[popuptextmanager] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = manager.pointerPairs[index].target;

            var outlinePointable = target.GetComponent<OutlinePointable>();
            var popupPointable = target.GetComponent<PopupPointable>();
            if (popupPointable == null || popupPointable == outlinePointable)
            {
                popupPointable = target.AddComponent<PopupPointable>();
            }

            if (popupPointable != null)
            {
                popupPointable.outlineMat = outlinePointable.outlineMat;
                popupPointable.popupManager = manager;
                popupPointable.index = index;

                DestroyImmediate(outlinePointable);

                Debug.Log(NAME + "update to popupPointable " + index.ToString());
            }

            if (popupPointable != null)
            {
                EditorUtility.SetDirty(popupPointable);
            }

            Debug.Log(NAME + BAR);
        }

        private void RevertOutlinePointable(int index, ref PopupTextManager manager)
        {
            const string NAME = "[popuptextmanager] ";
            const string BAR = "-----------------------------------";

            Debug.Log(NAME + BAR);

            var target = manager.pointerPairs[index].target;

            var outlinePointable = target.GetComponent<OutlinePointable>();
            var popupPointable = target.GetComponent<PopupPointable>();

            if (outlinePointable == null || outlinePointable == popupPointable)
            {
                outlinePointable = target.AddComponent<OutlinePointable>();
            }

            if (popupPointable != null)
            {
                Debug.Log(NAME + "revert to outline pointable " + index.ToString());

                outlinePointable.outlineMat = popupPointable.outlineMat;
                DestroyImmediate(popupPointable);
            }

            if (outlinePointable != null)
            {
                EditorUtility.SetDirty(outlinePointable);
            }

            Debug.Log(NAME + BAR);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PopupTextManager manager = target as PopupTextManager;

            if (GUILayout.Button("Overwrite to PopupPointable"))
            {
                for (int index = 0; index < manager.pointerPairs.Length; index++)
                {
                    OverwritePopupPointable(index, ref manager);
                }

                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Revert to OutlinePointable"))
            {
                for (int index = 0; index < manager.pointerPairs.Length; index++)
                {
                    RevertOutlinePointable(index, ref manager);
                }

                EditorUtility.SetDirty(manager);
            }
        }
    }
}