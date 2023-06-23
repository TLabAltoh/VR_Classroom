using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class PopupTextManager : MonoBehaviour
{
    [SerializeField] protected TextController[] m_controllers;
    [SerializeField] protected TextController[] m_onPointers;

    public TextController GetTextController(int index)
    {
        if (index < m_onPointers.Length)
            return m_onPointers[index];
        else
            return null;
    }

    private void OnDestroy()
    {
        // PopupTextManagerを破棄するとき，PopupTextManagerが保持しているTextController(とそれをもつGameObject)を一緒に破棄する．
        // TextControllerはStart()でtransform.parent = null(ペアレントを解除)しているので，PopupManagerを持つGameObjectを破棄
        // するだけでは何も起きない(TextControllerを持つGameObjectはシーンに残り続ける) ----> 以下の行で一緒に破棄すればいい．

        if(m_controllers != null)
            foreach(TextController controller in m_controllers) Destroy(controller.gameObject);

        if (m_onPointers != null)
            foreach (TextController controller in m_onPointers) Destroy(controller.gameObject);
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
            int index = 0;
            foreach(TLabOutlineSelectable outlineSelectable in manager.gameObject.GetComponentsInChildren<TLabOutlineSelectable>())
            {
                PopupSelectable popupSelectable = outlineSelectable.gameObject.GetComponent<PopupSelectable>();
                if (popupSelectable == null) popupSelectable = outlineSelectable.gameObject.AddComponent<PopupSelectable>();

                popupSelectable.OutlineMat      = outlineSelectable.OutlineMat;
                popupSelectable.PopupManager    = manager;
                popupSelectable.Index           = index++;

                if (outlineSelectable != null) DestroyImmediate(outlineSelectable);
                if (popupSelectable != null) EditorUtility.SetDirty(popupSelectable);
            }

            EditorUtility.SetDirty(manager);
        }

        if (GUILayout.Button("Revert to OutlineSelectable"))
        {
            foreach (PopupSelectable popupSelectable in manager.gameObject.GetComponentsInChildren<PopupSelectable>())
            {
                TLabOutlineSelectable outlineSelectable = popupSelectable.gameObject.GetComponent<TLabOutlineSelectable>();
                if (outlineSelectable == null || outlineSelectable == popupSelectable)
                    outlineSelectable = popupSelectable.gameObject.AddComponent<TLabOutlineSelectable>();

                outlineSelectable.OutlineMat = popupSelectable.OutlineMat;

                if (popupSelectable != null) DestroyImmediate(popupSelectable);
                if (outlineSelectable != null) EditorUtility.SetDirty(outlineSelectable);
            }

            EditorUtility.SetDirty(manager);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
#endregion CustomEditor