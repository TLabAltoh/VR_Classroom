using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class MiniTest : MonoBehaviour
{
    [SerializeField] private Image m_maru;
    [SerializeField] private Image m_batu;
    [SerializeField] private int m_corrent;

    private IEnumerator Seikai()
    {
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
        if (answer == m_corrent)
            Seikai();
        else
            FuSeikai();
    }
}
