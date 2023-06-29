using System.Collections;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MiniTest : MonoBehaviour
{
    [SerializeField] private TLabSyncAnim m_windowAnim;
    [SerializeField] private TLabSyncAnim[] m_graphs;

    [SerializeField] private ToggleGroup m_toggleGroup;
    [SerializeField] private Toggle m_corrent;

    [SerializeField] private Image m_maru;
    [SerializeField] private Image m_batu;

    private IEnumerator Seikai()
    {
        Debug.Log("Seikai");

        const float timer = 0.5f;
        float remain = 0.0f;
        Color prev;

        while (remain > 0.5f)
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

    public void AnswerCheck()
    {
        Debug.Log("Start Answer Check");

        Color prev;
        prev = m_batu.color;
        prev.a = 0;
        m_batu.color = prev;

        prev = m_maru.color;
        prev.a = 0;
        m_maru.color = prev;

        if (m_corrent.isOn)
            StartCoroutine("Seikai");
        else
            StartCoroutine("FuSeikai");

        MiniTestManager.Instance.RegistScore(m_corrent.isOn ? 100 : 0);
    }

    public void SwitchWindow(bool active)
    {
        m_windowAnim.SetBool("Switch", active);

        if (active == true) return;

        for (int i = 0; i < m_graphs.Length; i++)
        {
            TLabSyncAnim graph = m_graphs[i];
            graph.SetFloat("Ratio", MiniTestManager.Instance.GetScore(i + 1) / 100.0f);
            m_windowAnim.SyncAnim();
        }
    }

    private void Start()
    {
        m_windowAnim.SetBool("Switch", true);
        m_windowAnim.SyncAnim();
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

        //

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
