using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.InputField;

public class ClassroomEntry : MonoBehaviour
{
    [SerializeField] private TLabServerAddress m_serverAddrs;
    [SerializeField] private TLabInputField m_inputField;

    private IEnumerator ChangeScene()
    {
        float remain = 1.5f;
        while(remain > 0)
        {
            remain -= Time.deltaTime;
            yield return null;
        }

        string scene = CheckPassword() ? "Host" : "Guest";
        SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
    }

    public void EnterClassroom()
    {
        string ip = m_inputField.text.Split(" -p ")[0];
        m_serverAddrs.SetAddress("SyncServer", "ws://" + ip + ":5000");
        m_serverAddrs.SetAddress("Signaling", "ws://" + ip + ":3001");

        StartCoroutine("ChangeScene");
    }

    public void Exit()
    {
        Application.Quit();
    }

    private bool CheckPassword()
    {
        string target = m_inputField.text;
        string[] tmp = target.Split(" -p ");

        m_inputField.text = tmp[0];

        if (tmp.Length > 1)
            return tmp[1] == "1234";
        else
            return false;
    }
}
