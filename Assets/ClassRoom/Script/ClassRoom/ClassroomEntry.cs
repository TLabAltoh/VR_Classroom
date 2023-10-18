using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.InputField;
using TLab.Security;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace TLab.VRClassroom
{
    public class ClassroomEntry : MonoBehaviour
    {
        [SerializeField] private ServerAddressBox m_serverAddressBox;
        [SerializeField] private TLabInputField m_inputField;
        [SerializeField] private string m_password = "";
        [SerializeField] private string m_passHash = "";

        public string Password
        {
            get
            {
                return m_password;
            }

            set
            {
                m_password = value;
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
                m_passHash = value;
            }
        }

        private IEnumerator ChangeScene()
        {
            float remain = 1.5f;
            while (remain > 0)
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
            m_serverAddressBox.SetAddress("SyncServer", "ws://" + ip + ":5000");
            m_serverAddressBox.SetAddress("Signaling", "ws://" + ip + ":3001");

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
            {
                return TLabSecurity.GetHashString(tmp[1]) == m_passHash;
            }
            else
            {
                return false;
            }
        }
    }
}