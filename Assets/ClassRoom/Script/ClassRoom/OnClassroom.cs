using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OnClassroom : MonoBehaviour
{
    [SerializeField] private Transform m_centerEyeAnchor;
    [SerializeField] private Transform m_targetPanel;
    [SerializeField] private Transform m_webViewPanel;

    [SerializeField] private TLabSyncClient m_syncClient;
    [SerializeField] private TLabWebRTCVoiceChat m_voiceChat;

    private IEnumerator OnChangeScene()
    {
        // delete obj
        m_syncClient.RemoveAllGrabbers();
        m_syncClient.RemoveAllAnimators();

        yield return null;
        yield return null;

        // close socket
        m_voiceChat.CloseRTC();
        m_syncClient.CloseRTC();

        yield return null;
        yield return null;

        m_syncClient.Exit();

        yield return null;
        yield return null;

        float remain = 1.5f;
        while(remain > 0)
        {
            remain -= Time.deltaTime;
            yield return null;
        }

        SceneManager.LoadSceneAsync("Entry", LoadSceneMode.Single);
    }

    public void ExitClassroom()
    {
        StartCoroutine("OnChangeScene");
    }

    public void ShowReference()
    {
        SwitchPanel(m_webViewPanel);
    }

    private void SwitchPanel(Transform target, bool active)
    {
        target.gameObject.SetActive(active);

        target.transform.position = m_centerEyeAnchor.position + m_centerEyeAnchor.forward * 0.5f;

        if (active == true)
            target.LookAt(Camera.main.transform, Vector3.up);
    }

    private bool SwitchPanel(Transform target)
    {
        bool active = target.gameObject.activeSelf;
        SwitchPanel(target, !active);

        return active;
    }

    private void Start()
    {
        m_targetPanel.gameObject.SetActive(false);
    }

    private void Update()
    {
        if(OVRInput.GetDown(OVRInput.Button.Start) == true)
            if (SwitchPanel(m_targetPanel) == false)
                SwitchPanel(m_webViewPanel, false);
    }
}
