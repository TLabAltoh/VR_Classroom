using UnityEngine;
using UnityEngine.SceneManagement;

public class ClassroomEntry : MonoBehaviour
{
    [SerializeField] private TLabServerAddress m_serverAddrs;
    [SerializeField] private TLabInputField m_inputField;

    public void EnterClassroom()
    {
        string ip = m_inputField.text.Split(" -p ")[0];
        m_serverAddrs.SetAddress("SyncServer", "ws://" + ip + ":5000");
        m_serverAddrs.SetAddress("Signaling", "ws://" + ip + ":3001");
        m_serverAddrs.SetAddress("Shelf", "http://" + ip + ":5600/StandaloneWindows/testmodel.assetbundl");

        Invoke("ChangeScene", 1.5f);
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

    private void ChangeScene()
    {
        string scene = CheckPassword() ? "Host" : "Guest";
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
}
