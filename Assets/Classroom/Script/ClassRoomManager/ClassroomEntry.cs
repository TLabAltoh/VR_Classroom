using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.Security;
using TLab.InputField;

namespace TLab.VRClassroom
{
    public class ClassroomEntry : MonoBehaviour
    {
        [SerializeField] private ServerAddressBox m_serverAddressBox;
        [SerializeField] private TLabInputField m_inputField;
        [SerializeField] private string m_password = "";
        [SerializeField] private string m_passwordHash = "";

        public static string ENTRY_SCENE = "Entry";
        public static string HOST_SCENE = "Host";
        public static string GUEST_SCENE = "Guest";
        public static string DEMO_SCENE = "Demo_Scene";

        public static string SHELF_SERVER = "Shelf";
        public static string SYNC_SERVER = "SyncServer";
        public static string SIGNALING_SERVER = "Signaling";

        public string password { get => m_password; set => m_password = value; }

        public string passwordHash { get => m_passwordHash; set => m_passwordHash = value; }

        private IEnumerator ChangeScene(string scene)
        {
            yield return new WaitForSeconds(1.5f);

            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }

        private bool ConfirmPassword(string password)
        {
            return TLabSecurity.GetHashString(password) == m_passwordHash;
        }

        private Dictionary<string, string> ParseCommand(string argment)
        {
            var commandDic = new Dictionary<string, string>();

            // -a 123 -b 123 -c 123 ...
            var splited = argment.Split("-");

#if UNITY_EDITOR
            foreach (string command in splited)
            {
                Debug.Log("command: " + command);
            }
#endif

            for (int i = 1; i < splited.Length; i++)
            {
                var key = splited[i].Split(" ")[0];
                var value = splited[i].Split(" ")[1];

                commandDic[key] = value;
            }

            return commandDic;
        }

        public void EnterDemoScene()
        {
            var splited = m_inputField.text.Split(" ");
            var ipAddr = splited[0];
            var argment = "";
            for (int i = 1; i < splited.Length; i++)
            {
                argment += splited[i] + " ";
            }

            var commands = ParseCommand(argment);

            string scene = DEMO_SCENE;

            m_serverAddressBox.SetAddress(SYNC_SERVER, ipAddr, "5000");
            m_serverAddressBox.SetAddress(SIGNALING_SERVER, ipAddr, "3001");

            StartCoroutine(ChangeScene(scene));
        }

        public void Enter()
        {
            // 192.168.1.1 -p 1234 -a 1234 -b 1234 ...
            var splited = m_inputField.text.Split(" ");
            var ipAddr = splited[0];
            var argment = "";
            for (int i = 1; i < splited.Length; i++)
            {
                argment += splited[i] + " ";
            }

            var commands = ParseCommand(argment);
            string scene = GUEST_SCENE;

            if (commands.ContainsKey("p"))
            {
                var password = commands["p"];

                scene = ConfirmPassword(password) ? HOST_SCENE : scene;
            }

            m_serverAddressBox.SetAddress(SYNC_SERVER, ipAddr, "5000");
            m_serverAddressBox.SetAddress(SIGNALING_SERVER, ipAddr, "3001");

            StartCoroutine(ChangeScene(scene));
        }

#if UNITY_EDITOR
        public void PasswordTest(string argments)
        {
            // 192.168.1.1 -p 1234 -a 1234 -b 1234 ...
            var splited = argments.Split(" ");
            var ipAddr = splited[0];
            var argment = "";
            for (int i = 1; i < splited.Length; i++)
            {
                argment += splited[i] + " ";
            }

            Debug.Log("argment:" + argment);

            var commands = ParseCommand(argment);
            var password = commands["p"];

            Debug.Log("password: " + password);

            string scene = ConfirmPassword(password) ? HOST_SCENE : GUEST_SCENE;

            Debug.Log("scene: " + scene);
        }
#endif

        public void Exit()
        {
            Application.Quit();
        }
    }
}