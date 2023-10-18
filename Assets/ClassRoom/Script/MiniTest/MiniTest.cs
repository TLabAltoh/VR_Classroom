using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TLab.XR.VRGrabber;

namespace TLab.VRClassroom
{
    [RequireComponent(typeof(SyncAnimator))]
    public class MiniTest : MonoBehaviour
    {
        [SerializeField] private SyncAnimator m_windowAnim;
        [SerializeField] private SyncAnimator[] m_graphs;

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
            {
                StartCoroutine("Seikai");
            }
            else
            {
                StartCoroutine("FuSeikai");
            }

            MiniTestManager.Instance.RegistScore(m_corrent.isOn ? 100 : 0);
        }

        public void SwitchWindow(bool active)
        {
            m_windowAnim.SetBool("Switch", active);

            if (active)
            {
                return;
            }

            for (int i = 0; i < m_graphs.Length; i++)
            {
                SyncAnimator graph = m_graphs[i];
                graph.SetFloat("Ratio", MiniTestManager.Instance.GetScore(i + 1) / 100.0f);
            }
        }

        void Reset()
        {
            if (m_windowAnim == null)
            {
                m_windowAnim = GetComponent<SyncAnimator>();
            }
        }

        private void Start()
        {
            m_windowAnim.SetBool("Switch", true);
        }
    }
}
