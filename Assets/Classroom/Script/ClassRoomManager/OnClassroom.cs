using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.XR.Network;
using TLab.XR.Interact;
using TLab.Network.VoiceChat;

namespace TLab.VRClassroom
{
    public class OnClassroom : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private Transform m_centerEyeAnchor;
        [SerializeField] private Transform m_keyborad;
        [SerializeField] private Transform m_targetPanel;
        [SerializeField] private Transform m_webViewPanel;

        [Header("Network")]
        [SerializeField] private SyncClient m_syncClient;
        [SerializeField] private VoiceChat m_voiceChat;

        private IEnumerator ExitClassroomTask()
        {
            // delete obj

            ExclusiveController.ClearRegistry();
            SyncAnimator.ClearRegistry();

            yield return new WaitForSeconds(0.5f);

            // close socket
            m_voiceChat.CloseRTC();
            m_syncClient.CloseRTC();

            yield return new WaitForSeconds(0.5f);

            m_syncClient.Exit();

            yield return new WaitForSeconds(2.5f);
        }

        private IEnumerator ReEnterClassroomTask()
        {
            string scene = SyncClient.Instance.isHost ? ClassroomEntry.HOST_SCENE : ClassroomEntry.GUEST_SCENE;

            yield return ExitClassroomTask();

            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);

            yield break;
        }

        private IEnumerator BackToTheEntryTask()
        {
            yield return ExitClassroomTask();

            SceneManager.LoadSceneAsync(ClassroomEntry.ENTRY_SCENE, LoadSceneMode.Single);

            yield break;
        }

        public void ReEnter() => StartCoroutine(ReEnterClassroomTask());

        public void ExitClassroom() => StartCoroutine(BackToTheEntryTask());

        public void ShowWebView() => SwitchPanel(m_webViewPanel);

        /// <summary>
        /// Change the panel to the desired state
        /// </summary>
        /// <param name="target"></param>
        /// <param name="active"></param>
        private void SwitchPanel(Transform target, bool active)
        {
            target.gameObject.SetActive(active);

            target.transform.position = m_centerEyeAnchor.position + m_centerEyeAnchor.forward * 0.5f;

            if (active == true)
            {
                target.LookAt(Camera.main.transform, Vector3.up);
            }
        }

        /// <summary>
        /// Reverses the state of the panel
        /// </summary>
        /// <param name="target"></param>
        /// <returns></returns>
        private bool SwitchPanel(Transform target)
        {
            bool active = target.gameObject.activeSelf;
            SwitchPanel(target, !active);

            return active;
        }

        private void Start()
        {
            SwitchPanel(m_targetPanel, false);
        }

        private void Update()
        {
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                if (!SwitchPanel(m_targetPanel))
                {
                    SwitchPanel(m_webViewPanel, false);
                }
            }
        }
    }
}
