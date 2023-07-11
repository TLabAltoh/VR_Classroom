using UnityEngine;
using UnityEngine.SceneManagement;

public class OnClassroom : MonoBehaviour
{
    void ChangeScene()
    {
        SceneManager.LoadScene("Entry", LoadSceneMode.Single);
    }

    public void ExitClassroom()
    {
        Invoke("ChangeScene", 1.5f);
    }
}
