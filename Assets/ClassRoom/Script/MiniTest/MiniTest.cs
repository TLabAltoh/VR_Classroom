using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MiniTest : MonoBehaviour
{
    [SerializeField] private Image m_maru;
    [SerializeField] private Image m_batu;
    [SerializeField] private int m_corrent = 0;

    private IEnumerator Seikai()
    {
        Debug.Log("Seikai");

        const float timer = 0.5f;
        float remain = 0.0f;
        Color prev;

        while(remain > 0.5f)
        {
            remain += Time.deltaTime;
            prev = m_maru.color;
            prev.a = remain / timer;
            m_maru.color = prev;
            yield return null;
        }

        prev = m_maru.color;
        prev.a = 1.0f;
        m_maru.color = prev;
    }

    private IEnumerator FuSeikai()
    {
        Debug.Log("Fseikai");

        const float timer = 0.5f;
        float remain = 0.0f;
        Color prev;

        while (remain > 0.5f)
        {
            remain += Time.deltaTime;
            prev = m_batu.color;
            prev.a = remain / timer;
            m_batu.color = prev;
            yield return null;
        }

        prev = m_batu.color;
        prev.a = 1.0f;
        m_batu.color = prev;
    }

    public void AnswerCheck(int answer)
    {
        Debug.Log("Start Answer Check");

        Color prev;
        prev = m_batu.color;
        prev.a = 0;
        m_batu.color = prev;

        prev = m_maru.color;
        prev.a = 0;
        m_maru.color = prev;

        if (answer == m_corrent)
            StartCoroutine("Seikai");
        else
            StartCoroutine("FuSeikai");
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(MiniTest))]
public class MiniTestEditor: Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        if (GUILayout.Button("MiniTest"))
        {
            MiniTest miniTest = target as MiniTest;
            miniTest.AnswerCheck(1);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
