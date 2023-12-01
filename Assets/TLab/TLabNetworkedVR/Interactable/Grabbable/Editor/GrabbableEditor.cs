using UnityEngine;
using UnityEditor;


namespace TLab.XR.Interact.Editor
{
    [CustomEditor(typeof(Grabbable))]
    [CanEditMultipleObjects]
    public class GrabbableEditor : UnityEditor.Editor
    {
        private void InitializeForRotateble(Grabbable grabbable, Rotatable rotatable)
        {
            grabbable.InitializeRotatable();
            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(rotatable);
        }

        private void InitializeForDivibable(Grabbable grabbable, bool isRoot)
        {
            // Disable Rigidbody.useGrabity
            grabbable.enableSync = true;
            grabbable.UseRigidbody(false, false);

            grabbable.gameObject.layer = LayerMask.NameToLayer("TLabGrabbable");

            var meshFilter = grabbable.gameObject.RequireComponent<MeshFilter>();
            var rotatable = grabbable.gameObject.RequireComponent<Rotatable>();
            var meshCollider = grabbable.gameObject.RequireComponent<MeshCollider>();
            meshCollider.enabled = isRoot;

            EditorUtility.SetDirty(grabbable);
            EditorUtility.SetDirty(rotatable);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var grabbable = target as Grabbable;
            var rotatable = grabbable.gameObject.GetComponent<Rotatable>();

            if (rotatable != null && GUILayout.Button("Initialize for Rotatable"))
            {
                InitializeForRotateble(grabbable, rotatable);
            }

            if (grabbable.enableDivide == true && GUILayout.Button("Initialize for Devibable"))
            {
                InitializeForDivibable(grabbable, true);

                foreach (GameObject divideTarget in grabbable.divideTargets)
                {
                    var grabbableChild = divideTarget.gameObject.RequireComponent<Grabbable>();
                    InitializeForDivibable(grabbableChild, false);
                }
            }
        }
    }
}
