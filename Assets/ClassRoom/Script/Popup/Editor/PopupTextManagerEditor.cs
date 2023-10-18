using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using TLab.XR.VRGrabber.VFX;

namespace TLab.VRClassroom.Editor
{
    [CustomEditor(typeof(PopupTextManager))]
    public class PopupTextManagerEditor : UnityEditor.Editor
    {
        private void OverwritePopuoSelectable(int index, ref PopupTextManager manager)
        {
            Debug.Log("[popuptextmanager] -----------------------------------");

            GameObject target = manager.PointerPairs[index].target;

            var outlineSelectable = target.GetComponent<OutlineSelectable>();
            var popupSelectable = target.GetComponent<PopupSelectable>();
            if (popupSelectable == null || popupSelectable == outlineSelectable)
            {
                popupSelectable = target.AddComponent<PopupSelectable>();
            }

            if (outlineSelectable != null)
            {
                popupSelectable.OutlineMat = outlineSelectable.OutlineMat;
                popupSelectable.PopupManager = manager;
                popupSelectable.Index = index;

                DestroyImmediate(outlineSelectable);

                Debug.Log("[popuptextmanager] update to popupselectable " + index.ToString());
            }

            if (popupSelectable != null)
            {
                EditorUtility.SetDirty(popupSelectable);
            }

            Debug.Log("[popuptextmanager] -----------------------------------");
        }

        private void RevertOutlineSelectable(int index, ref PopupTextManager manager)
        {
            Debug.Log("[popuptextmanager] -----------------------------------");

            GameObject target = manager.PointerPairs[index].target;

            var outlineSelectable = target.GetComponent<OutlineSelectable>();
            var popupSelectable = target.GetComponent<PopupSelectable>();

            if (outlineSelectable == null || outlineSelectable == popupSelectable)
            {
                outlineSelectable = target.AddComponent<OutlineSelectable>();
            }

            if (popupSelectable != null)
            {
                Debug.Log("[popuptextmanager] revert to outline selectable " + index.ToString());

                outlineSelectable.OutlineMat = popupSelectable.OutlineMat;
                DestroyImmediate(popupSelectable);
            }

            if (outlineSelectable != null)
            {
                EditorUtility.SetDirty(outlineSelectable);
            }

            Debug.Log("[popuptextmanager] -----------------------------------");
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            PopupTextManager manager = target as PopupTextManager;

            if (GUILayout.Button("Overwrite to PopupSelectable"))
            {
                for (int index = 0; index < manager.PointerPairs.Length; index++)
                {
                    OverwritePopuoSelectable(index, ref manager);
                }

                EditorUtility.SetDirty(manager);
            }

            if (GUILayout.Button("Revert to OutlineSelectable"))
            {
                for (int index = 0; index < manager.PointerPairs.Length; index++)
                {
                    RevertOutlineSelectable(index, ref manager);
                }

                EditorUtility.SetDirty(manager);
            }
        }
    }
}