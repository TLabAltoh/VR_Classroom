using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.InputField;
using TLab.XR.Network;
using TLab.XR.Network.Util;
using TLab.XR.Network.Security;

namespace TLab.VRClassroom
{
    public class Entrance : MonoBehaviour
    {
        [SerializeField] private RoomConfig m_roomConfig;
        [SerializeField] private TLabInputField m_input;

        public string THIS_NAME => "[" + this.GetType() + "] ";

        private IEnumerator ChangeScene(string scene)
        {
            yield return new WaitForSeconds(1.5f);

            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);
        }

        public void UpdateConfig(string ipAddr)
        {
            m_roomConfig.syncSeverAddr.UpdateConfig(ipAddr, "5000");
            m_roomConfig.signalingServerAddr.UpdateConfig(ipAddr, "3001");
        }

        public string GetIPAddr()
        {
            var splited = m_input.text.Split(" ");

            return splited[0];
        }

        public Dictionary<string, string> GetOptions()
        {
            // 192.168.1.1 -p 1234 -a 1234 -b 1234 ...

            var splited = m_input.text.Split(" ");

            var argment = "";
            for (int i = 1; i < splited.Length; i++)
            {
                argment += splited[i] + " ";
            }

            var options = Command.ParseCommand(argment);

            return options;
        }

        public void EnterDemoScene()
        {
            string scene = Classroom.DEMO_SCENE;

            UpdateConfig(GetIPAddr());

            StartCoroutine(ChangeScene(scene));
        }

        public void Enter()
        {
            var ipAddr = GetIPAddr();
            var options = GetOptions();

            var scene = Classroom.GUEST_SCENE;

            if (options.ContainsKey("p"))
            {
                var password = options["p"];

                scene = Authentication.ConfirmPassword(password, m_roomConfig.passwordHash) ? Classroom.HOST_SCENE : scene;
            }

            UpdateConfig(ipAddr);

            StartCoroutine(ChangeScene(scene));
        }

#if UNITY_EDITOR
        public void PasswordTest(string argments)
        {
            var ipAddr = GetIPAddr();

            Debug.Log(THIS_NAME + $"Ip addr: {ipAddr}");

            var options = GetOptions();

            var password = options["p"];

            Debug.Log(THIS_NAME + $"Password: {password}");

            var scene = Authentication.ConfirmPassword(password, m_roomConfig.passwordHash) ? Classroom.HOST_SCENE : Classroom.GUEST_SCENE;

            Debug.Log(THIS_NAME + $"Scene: {scene}");
        }
#endif

        public void Exit()
        {
            Application.Quit();
        }
    }
}