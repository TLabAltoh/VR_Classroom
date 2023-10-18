using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using static TLab.XR.VRGrabber.Utility.ComponentExtention;

namespace TLab.XR.VRGrabber.Editor
{
    [CustomEditor(typeof(TLabSyncGrabbable))]
    [CanEditMultipleObjects]

    public class TLabSyncGrabbableEditor : UnityEditor.Editor
    {
        private void InitializeForRotateble(TLabSyncGrabbable grabbable, TLabVRRotatable rotatable)
        {
            grabbable.InitializeRotatable();
            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(rotatable);
        }

        private void InitializeForDivibable(TLabSyncGrabbable grabbable, bool isRoot)
        {
            // Disable Rigidbody.useGrabity
            grabbable.m_enableSync = true;
            grabbable.m_autoSync = false;
            grabbable.m_locked = false;
            grabbable.UseRigidbody(false, false);

            grabbable.gameObject.layer = LayerMask.NameToLayer("TLabGrabbable");

            var meshFilter = grabbable.gameObject.RequireComponent<MeshFilter>();
            var rotatable = grabbable.gameObject.RequireComponent<TLabSyncRotatable>();
            var meshCollider = grabbable.gameObject.RequireComponent<MeshCollider>();
            meshCollider.enabled = isRoot;

            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(rotatable);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TLabSyncGrabbable grabbable = target as TLabSyncGrabbable;
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
                    var grabbableChild = divideTarget.gameObject.RequireComponent<TLabSyncGrabbable>();
                    InitializeForDivibable(grabbableChild, false);
                }
            }
        }
    }
}
