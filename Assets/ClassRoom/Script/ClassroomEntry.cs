using UnityEngine;
using UnityEngine.SceneManagement;

public class ClassroomEntry : MonoBehaviour
{
    [SerializeField] private TLabServerAddress m_serverAddrs;
    [SerializeField] private TLabInputField m_inputField;
    [SerializeField] private bool m_isHost = false;

    public void EnterClassroom()
    {
        string ip = m_inputField.text;
        m_serverAddrs.SetAddress("SyncServer", "ws://" + ip + ":5000");
        m_serverAddrs.SetAddress("Signaling", "ws://" + ip + ":3001");
        m_serverAddrs.SetAddress("Shelf", "http://" + ip + ":5600/StandaloneWindows/testmodel.assetbundl");

        Invoke("ChangeScene", 1.5f);
    }

    public void Exit()
    {
        Application.Quit();
    }

    void ChangeScene()
    {
        string scene = m_isHost ? "Host" : "Guest";
        SceneManager.LoadScene(scene, LoadSceneMode.Single);
    }
}
