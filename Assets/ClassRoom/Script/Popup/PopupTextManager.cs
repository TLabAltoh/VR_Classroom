using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PopupTextManager : MonoBehaviour
{
    [System.Serializable]
    public class PointerPopupPair
    {
        public TextController controller;
        public GameObject target;
    }

    public PointerPopupPair[] PointerPairs
    {
        get
        {
            return m_pointerPairs;
        }
    }

    [SerializeField] protected TextController[] m_controllers;
    [SerializeField] protected PointerPopupPair[] m_pointerPairs;

    public TextController GetTextController(int index)
    {
        if (index < m_pointerPairs.Length)
            return m_pointerPairs[index].controller;
        else
            return null;
    }

    private IEnumerator LateStart()
    {
        yield return null;

        foreach (PointerPopupPair popupPair in m_pointerPairs)
            popupPair.controller.FadeOutImmidiately();

        yield break;
    }

    private void Start()
    {
        StartCoroutine("LateStart");
    }

    private void OnDestroy()
    {
        // PopupTextManagerを破棄するとき，PopupTextManagerが保持しているTextController(とそれをもつGameObject)を一緒に破棄する．
        // TextControllerはStart()でtransform.parent = null(ペアレントを解除)しているので，PopupManagerを持つGameObjectを破棄
        // するだけでは何も起きない(TextControllerを持つGameObjectはシーンに残り続ける) ----> 以下の行で一緒に破棄すればいい．

        if(m_controllers.Length > 0)
            foreach(TextController controller in m_controllers) Destroy(controller.gameObject);

        if (m_pointerPairs.Length > 0)
            foreach (PointerPopupPair pointerPair in m_pointerPairs) Destroy(pointerPair.controller.gameObject);
    }
}

#region CustomEditor
#if UNITY_EDITOR
[CustomEditor(typeof(PopupTextManager))]
public class PopupTextManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        PopupTextManager manager = target as PopupTextManager;

        if (GUILayout.Button("Overwrite Outline for Popup Selectable"))
        {
            Debug.Log("[popuptextmanager] -----------------------------------");

            for (int index = 0; index < manager.PointerPairs.Length; index++)
            {
                GameObject target = manager.PointerPairs[index].target;

                TLabOutlineSelectable outlineSelectable = target.GetComponent<TLabOutlineSelectable>();
                PopupSelectable popupSelectable         = target.GetComponent<PopupSelectable>();
                if (popupSelectable == null || popupSelectable == outlineSelectable)
                    popupSelectable = target.AddComponent<PopupSelectable>();

                if (outlineSelectable != null)
                {
                    popupSelectable.OutlineMat      = outlineSelectable.OutlineMat;
                    popupSelectable.PopupManager    = manager;
                    popupSelectable.Index           = index;

                    DestroyImmediate(outlineSelectable);

                    Debug.Log("[popuptextmanager] update to popupselectable " + index.ToString());
                }

                if (popupSelectable != null) EditorUtility.SetDirty(popupSelectable);
            }

            Debug.Log("[popuptextmanager] -----------------------------------");

            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Revert to OutlineSelectable"))
        {
            Debug.Log("[popuptextmanager] -----------------------------------");

            for (int index = 0; index < manager.PointerPairs.Length; index++)
            {
                GameObject target = manager.PointerPairs[index].target;

                TLabOutlineSelectable outlineSelectable = target.GetComponent<TLabOutlineSelectable>();
                PopupSelectable popupSelectable         = target.GetComponent<PopupSelectable>();
                if (outlineSelectable == null || outlineSelectable == popupSelectable)
                    outlineSelectable = target.AddComponent<TLabOutlineSelectable>();

                if (popupSelectable != null)
                {
                    Debug.Log("[popuptextmanager] revert to outline selectable " + index.ToString());

                    outlineSelectable.OutlineMat = popupSelectable.OutlineMat;
                    DestroyImmediate(popupSelectable);
                }

                if (outlineSelectable != null) EditorUtility.SetDirty(outlineSelectable);
            }

            Debug.Log("[popuptextmanager] -----------------------------------");

            EditorUtility.SetDirty(manager);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
#endregion CustomEditor