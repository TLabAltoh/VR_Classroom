using System.Collections;
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

    protected IEnumerator LateStart()
    {
        yield return null;

        foreach (PointerPopupPair popupPair in m_pointerPairs)
            popupPair.controller.FadeOutImmidiately();

        yield break;
    }

    protected void Start()
    {
        StartCoroutine("LateStart");
    }

    protected void OnDestroy()
    {
        if (m_controllers.Length > 0)
            foreach(TextController controller in m_controllers)
                if(controller != null) Destroy(controller.gameObject);

        if (m_pointerPairs.Length > 0)
            foreach (PointerPopupPair pointerPair in m_pointerPairs)
                if(pointerPair.controller != null) Destroy(pointerPair.controller.gameObject);
    }
}

#region CustomEditor
#if UNITY_EDITOR
[CustomEditor(typeof(PopupTextManager))]
public class PopupTextManagerEditor : Editor
{
    private void OverwritePopuoSelectable(int index, ref PopupTextManager manager)
    {
        Debug.Log("[popuptextmanager] -----------------------------------");

        GameObject target = manager.PointerPairs[index].target;

        TLabOutlineSelectable outlineSelectable = target.GetComponent<TLabOutlineSelectable>();
        PopupSelectable popupSelectable = target.GetComponent<PopupSelectable>();
        if (popupSelectable == null || popupSelectable == outlineSelectable)
            popupSelectable = target.AddComponent<PopupSelectable>();

        if (outlineSelectable != null)
        {
            popupSelectable.OutlineMat = outlineSelectable.OutlineMat;
            popupSelectable.PopupManager = manager;
            popupSelectable.Index = index;

            DestroyImmediate(outlineSelectable);

            Debug.Log("[popuptextmanager] update to popupselectable " + index.ToString());
        }

        if (popupSelectable != null) EditorUtility.SetDirty(popupSelectable);

        Debug.Log("[popuptextmanager] -----------------------------------");
    }

    private void RevertOutlineSelectable(int index, ref PopupTextManager manager)
    {
        Debug.Log("[popuptextmanager] -----------------------------------");

        GameObject target = manager.PointerPairs[index].target;

        TLabOutlineSelectable outlineSelectable = target.GetComponent<TLabOutlineSelectable>();
        PopupSelectable popupSelectable = target.GetComponent<PopupSelectable>();
        if (outlineSelectable == null || outlineSelectable == popupSelectable)
            outlineSelectable = target.AddComponent<TLabOutlineSelectable>();

        if (popupSelectable != null)
        {
            Debug.Log("[popuptextmanager] revert to outline selectable " + index.ToString());

            outlineSelectable.OutlineMat = popupSelectable.OutlineMat;
            DestroyImmediate(popupSelectable);
        }

        if (outlineSelectable != null) EditorUtility.SetDirty(outlineSelectable);

        Debug.Log("[popuptextmanager] -----------------------------------");
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        PopupTextManager manager = target as PopupTextManager;

        if (GUILayout.Button("Overwrite to PopupSelectable"))
        {
            for (int index = 0; index < manager.PointerPairs.Length; index++)
                OverwritePopuoSelectable(index, ref manager);

            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Revert to OutlineSelectable"))
        {
            for (int index = 0; index < manager.PointerPairs.Length; index++)
                RevertOutlineSelectable(index, ref manager);

            EditorUtility.SetDirty(manager);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
#endregion CustomEditor