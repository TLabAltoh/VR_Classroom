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

    private void ChangeScene()
    {
        SceneManager.LoadScene("Entry", LoadSceneMode.Single);
    }

    private IEnumerator OnChangeScene()
    {
        // delete obj
        m_syncClient.RemoveAllGrabbers();
        m_syncClient.RemoveAllAnimators();

        yield return null;
        yield return null;

        // close socket
        m_voiceChat.Close();
        m_syncClient.Close();

        yield return null;
        yield return null;

        Invoke("ChangeScene", 1.5f);
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

        if (active == true)
        {
            target.position = m_centerEyeAnchor.position + m_centerEyeAnchor.forward * 1.0f;
            target.up = (m_centerEyeAnchor.position - target.position).normalized;

            Vector3 rotateAxis = Vector3.Cross(target.right, Vector3.up);
            target.rotation = Quaternion.AngleAxis(rotateAxis.magnitude * 360.0f, rotateAxis) * target.rotation;
        }
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
