using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.InputField;
using TLab.Security;

namespace TLab.VRClassroom
{
    public class ClassroomEntry : MonoBehaviour
    {
        [SerializeField] private ServerAddressBox m_serverAddressBox;
        [SerializeField] private TLabInputField m_inputField;
        [SerializeField] private string m_password = "";
        [SerializeField] private string m_passwordHash = "";

        private static string HOST_SCENE = "Host";
        private static string GUEST_SCENE = "Guest";

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
            foreach (string command in splited)
            {
                var key = command.Split(" ")[0];
                var value = command.Split(" ")[2];

                commandDic[key] = value;
            }

            return commandDic;
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
            var password = commands["p"];

            string scene = ConfirmPassword(password) ? HOST_SCENE : GUEST_SCENE;

            m_serverAddressBox.SetAddress(SYNC_SERVER, ipAddr, "5000");
            m_serverAddressBox.SetAddress(SIGNALING_SERVER, ipAddr, ":3001");

            StartCoroutine(ChangeScene(scene));
        }

        public void Exit()
        {
            Debug.Log("---------");
            Application.Quit();
        }
    }
}