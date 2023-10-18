using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static TLab.XR.VRGrabber.Utility.ComponentExtention;

namespace TLab.XR.VRGrabber.Editor
{
    [CustomEditor(typeof(TLabVRGrabbable))]

    public class TLabVRGrabbableEditor : UnityEditor.Editor
    {
        private void InitializeForRotateble(TLabVRGrabbable grabbable, TLabVRRotatable rotatable)
        {
            grabbable.InitializeRotatable();
            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(rotatable);
        }

        private void InitializeForDivibable(TLabVRGrabbable grabbable, bool isRoot)
        {
            // Rigidbody‚ÌUseGravity‚ð–³Œø‰»‚·‚é
            grabbable.UseRigidbody(false, false);

            grabbable.gameObject.layer = LayerMask.NameToLayer("TLabGrabbable");

            var meshFilter = grabbable.gameObject.RequireComponent<MeshFilter>();
            var rotatable = grabbable.gameObject.RequireComponent<TLabVRRotatable>();
            var meshCollider = grabbable.gameObject.RequireComponent<MeshCollider>();
            meshCollider.enabled = isRoot;

            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(rotatable);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TLabVRGrabbable grabbable = target as TLabVRGrabbable;

            TLabVRRotatable rotatable = grabbable.gameObject.GetComponent<TLabVRRotatable>();

            if (rotatable != null && GUILayout.Button("Initialize for Rotatable"))
            {
                InitializeForRotateble(grabbable, rotatable);
            }

            if (grabbable.EnableDivide == true && GUILayout.Button("Initialize for Devibable"))
            {
                InitializeForDivibable(grabbable, true);

                foreach (GameObject divideTarget in grabbable.DivideTargets)
                {
                    TLabVRGrabbable grabbableChild = divideTarget.RequireComponent<TLabVRGrabbable>();

                    InitializeForDivibable(grabbableChild, false);
                }
            }
        }
    }
}
