using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.XR.VRGrabber.Editor
{
    [CustomEditor(typeof(TLabVRHandManager))]
    public class TLabVRHandManagerEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            TLabVRHandManager manager = target as TLabVRHandManager;

            bool updated = false;
            bool controller = false;
            bool customHand = false;
            bool hand = false;
            Transform right = null;
            Transform left = null;

            if (GUILayout.Button("Switch Hand"))
            {
                right = manager.VRControllerRight.transform;
                left = manager.VRControllerLeft.transform;
                hand = true;
                updated = true;

                Debug.Log("Player Operation Method Changed: Hand");
            }

            if (GUILayout.Button("Switch Custom Hand"))
            {
                right = manager.VRCustomHandRight.transform;
                left = manager.VRCustomHandLeft.transform;
                customHand = true;
                updated = true;

                Debug.Log("Player Operation Method Changed: Custom Controller");
            }

            if (GUILayout.Button("Switch Controller"))
            {
                right = manager.VRControllerRight.transform;
                left = manager.VRControllerLeft.transform;
                controller = true;
                updated = true;

                Debug.Log("Player Operation Method Changed: Controller");
            }

            if (updated)
            {
                manager.VRControllerHandRight.enabled = controller;
                manager.VRControllerHandLeft.enabled = controller;
                manager.VRTrackingHandRight.enabled = hand || customHand;
                manager.VRTrackingHandLeft.enabled = hand || customHand;

                if (hand || customHand)
                {
                    manager.ProjectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.HandsOnly;
                }
                else
                {
                    manager.ProjectConfig.handTrackingSupport = OVRProjectConfig.HandTrackingSupport.ControllersOnly;
                }

                manager.VRControllerRight.SetActive(controller);
                manager.VRControllerLeft.SetActive(controller);
                manager.VRCustomHandRight.SetActive(customHand);
                manager.VRCustomHandLeft.SetActive(customHand);
                manager.VRHandRight.SetActive(hand);
                manager.VRHandLeft.SetActive(hand);

                EditorUtility.SetDirty(manager.ProjectConfig);

                EditorUtility.SetDirty(manager.VRControllerHandRight);
                EditorUtility.SetDirty(manager.VRControllerHandLeft);
                EditorUtility.SetDirty(manager.VRTrackingHandRight);
                EditorUtility.SetDirty(manager.VRTrackingHandLeft);

                EditorUtility.SetDirty(manager.VRControllerRight);
                EditorUtility.SetDirty(manager.VRControllerLeft);
                EditorUtility.SetDirty(manager.VRCustomHandRight);
                EditorUtility.SetDirty(manager.VRCustomHandLeft);
                EditorUtility.SetDirty(manager.VRHandRight);
                EditorUtility.SetDirty(manager.VRHandLeft);
            }
        }
    }
}
