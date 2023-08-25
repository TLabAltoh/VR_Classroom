using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using TLab.InputField;
using TLab.Security;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ClassroomEntry : MonoBehaviour
{
    [SerializeField] private TLabServerAddress m_serverAddrs;
    [SerializeField] private TLabInputField m_inputField;
    [SerializeField] private string password = "";
    [SerializeField] private string passHash = "";

    public string Password
    {
        get
        {
            return password;
        }

        set
        {
            password = value;
        }
    }

    public string PassHash
    {
        get
        {
            return PassHash;
        }

        set
        {
            passHash = value;
        }
    }

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
            return TLabSecurity.GetHashString(tmp[1]) == passHash;
        else
            return false;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ClassroomEntry))]
[CanEditMultipleObjects]

public class ClassroomEntryEditor : Editor
{

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        ClassroomEntry classroomEntry = target as ClassroomEntry;

        if(GUILayout.Button("Regist Password"))
        {
            string hash = TLabSecurity.GetHashString(classroomEntry.Password);
            classroomEntry.PassHash = hash;
            classroomEntry.Password = "";

            EditorUtility.SetDirty(classroomEntry);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif