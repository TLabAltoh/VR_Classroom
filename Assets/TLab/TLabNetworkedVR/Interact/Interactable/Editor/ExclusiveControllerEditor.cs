using UnityEngine;
using UnityEditor;

#if TLAB_WITH_OCULUS_SDK
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;
#endif

namespace TLab.XR.Interact.Editor
{
    [CustomEditor(typeof(ExclusiveController))]
    [CanEditMultipleObjects]
    public class ExclusiveControllerEditor : UnityEditor.Editor
    {
        // Editor created on the assumption that the controller
        // uses the grabbableHandle and rotateble; modify as
        // appropriate to suit your needs.

        private void InitializeForRotateble(ExclusiveController controller, Rotatable rotatable)
        {
            controller.InitializeRotatable();
            rotatable.rotateSpeed = 10f;
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(rotatable);
        }

        private void InitializeForDivibable(GameObject target, bool isRoot)
        {
            var meshFilter = target.RequireComponent<MeshFilter>();

            var meshCollider = target.RequireComponent<MeshCollider>();
            meshCollider.enabled = isRoot;
            meshCollider.convex = true;     // meshCollider.ClosestPoint only works with convex = true

            var controller = target.RequireComponent<ExclusiveController>();
            controller.enableSync = true;
            controller.CreateHashID();
            controller.UseRigidbody(false, false);  // Disable Rigidbody.useGrabity

            var grabbable = target.RequireComponent<Grabbable>();
            grabbable.enableCollision = true;

            var rotatable = target.RequireComponent<Rotatable>();
            rotatable.enableCollision = true;
            rotatable.rotateSpeed = 10f;

#if TLAB_WITH_OCULUS_SDK
            var rayInteractable = target.RequireComponent<RayInteractable>();
            var colliderSurface = target.RequireComponent<ColliderSurface>();
#endif

            EditorUtility.SetDirty(meshFilter);
            EditorUtility.SetDirty(meshCollider);
            EditorUtility.SetDirty(controller);
            EditorUtility.SetDirty(rotatable);
            EditorUtility.SetDirty(grabbable);

#if TLAB_WITH_OCULUS_SDK
            EditorUtility.SetDirty(rayInteractable);
            EditorUtility.SetDirty(colliderSurface);
#endif
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
                InitializeForDivibable(controller.gameObject, true);

                foreach (var divideTarget in controller.divideTargets)
                {
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(divideTarget);

                    InitializeForDivibable(divideTarget, false);

                    var tlabGrabbableChild = divideTarget.gameObject.GetComponent<VRGrabber.TLabVRGrabbable>();
                    if(tlabGrabbableChild != null)
                    {
                        DestroyImmediate(tlabGrabbableChild);
                    }

                    var tlabRotatableChild = divideTarget.gameObject.GetComponent<VRGrabber.TLabVRRotatable>();
                    if (tlabRotatableChild != null)
                    {
                        DestroyImmediate(tlabRotatableChild);
                    }

                    EditorUtility.SetDirty(divideTarget);
                }
            }

            if (GUILayout.Button("Create Hash ID"))
            {
                controller.CreateHashID();
            }
        }
    }
}
