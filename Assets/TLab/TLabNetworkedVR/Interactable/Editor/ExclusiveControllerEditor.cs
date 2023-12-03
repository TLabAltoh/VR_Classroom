using UnityEngine;
using UnityEditor;


namespace TLab.XR.Interact.Editor
{
    [CustomEditor(typeof(ExclusiveController))]
    [CanEditMultipleObjects]
    public class ExclusiveControllerEditor : UnityEditor.Editor
    {
        private void InitializeForRotateble(ExclusiveController controller, Rotatable rotatable)
        {
            controller.InitializeRotatable();
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(rotatable);
        }

        private void InitializeForDivibable(ExclusiveController controller, bool isRoot)
        {
            // Disable Rigidbody.useGrabity
            controller.enableSync = true;
            controller.UseRigidbody(false, false);

            var meshFilter = controller.gameObject.RequireComponent<MeshFilter>();
            var rotatable = controller.gameObject.RequireComponent<Rotatable>();
            var meshCollider = controller.gameObject.RequireComponent<MeshCollider>();
            meshCollider.enabled = isRoot;

            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(rotatable);
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            var controller = target as ExclusiveController;

            var rotatable = controller.gameObject.GetComponent<Rotatable>();
            if (rotatable != null && GUILayout.Button("Initialize for Rotatable"))
            {
                InitializeForRotateble(controller, rotatable);
            }

            if (controller.enableDivide && GUILayout.Button("Initialize for Devibable"))
            {
                InitializeForDivibable(controller, true);

                foreach (var divideTarget in controller.divideTargets)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(divideTarget);

                    var controllerChild = divideTarget.gameObject.RequireComponent<ExclusiveController>();
                    InitializeForDivibable(controllerChild, false);

                    var tlabGrabbableChild = divideTarget.gameObject.GetComponent<VRGrabber.TLabVRGrabbable>();
                    if(tlabGrabbableChild != null)
                    {
                        DestroyImmediate(tlabGrabbableChild);
                    }

                    divideTarget.gameObject.RequireComponent<GrabbableHandle>();

                    var tlabRotatableChild = divideTarget.gameObject.GetComponent<VRGrabber.TLabVRRotatable>();
                    if (tlabRotatableChild != null)
                    {
                        DestroyImmediate(tlabRotatableChild);
                    }

                    divideTarget.gameObject.RequireComponent<Rotatable>();

                    EditorUtility.SetDirty(divideTarget);
                }
            }
        }
    }
}
