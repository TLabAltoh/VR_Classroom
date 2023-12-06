using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction;
using TLab.InputField;
using TLab.XR.Network;
using TLab.Network.VoiceChat;

namespace TLab.VRClassroom
{
    // �ڕW��TLab.XR.Input.InputDataSource�݂̂��C�v���O�C���Ɉˑ�����\�[�X�R�[�h�ɂ�����

    public class OnClassroom : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private Transform m_centerEyeAnchor;
        [SerializeField] private Transform m_targetPanel;
        [SerializeField] private Transform m_webViewPanel;

        // TODO: RayInteractable���Q�Ƃ��Ȃ����@����������
        [Header("Keyborad")]
        [SerializeField] private TLabVKeyborad m_keyborad;
        [SerializeField] private RayInteractable m_keyboradInteractable;

        [Header("Network")]
        [SerializeField] private SyncClient m_syncClient;
        [SerializeField] private VoiceChat m_voiceChat;

        private const float HALF = 0.5f;

        private Vector3 cameraPos => Camera.main.transform.position;

        private IEnumerator ExitClassroomTask()
        {
            // clear networked objct
            NetworkedObject.ClearRegistry();
            SyncAnimator.ClearRegistry();

            yield return new WaitForSeconds(0.5f);

            // close webrtc socket
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

        private Vector3 GetEyeDirectionPos(float xOffset = 0f, float yOffset = 0f, float zOffset = 0f)
        {
            return m_centerEyeAnchor.position +
                    m_centerEyeAnchor.right * xOffset +
                    m_centerEyeAnchor.up * yOffset +
                    m_centerEyeAnchor.forward * zOffset;
        }

        /// <summary>
        /// TLabVKeyborad.onHide�ŌĂяo���R�[���o�b�N
        /// </summary>
        /// <param name="keyborad"></param>
        /// <param name="hide"></param>
        public void OnHideKeyborad(TLabVKeyborad keyborad, bool hide)
        {
            var active = !hide;

            // enable or disable keyborad's ray interactable
            m_keyboradInteractable.enabled = active;

            if (active)
            {
                // �p�l�������������ɕ\��
                const float SCALE = 0.75f;
                var position = GetEyeDirectionPos(0f, -HALF * SCALE, HALF * SCALE);
                m_keyborad.SetTransform(position, cameraPos, Vector3.up);
            }
        }

        /// <summary>
        /// Change the panel to the desired state
        /// </summary>
        /// <param name="target"></param>
        /// <param name="active"></param>
        private void SwitchPanel(
            Transform target, bool active,
            float xOffset = 0f, float yOffset = 0f, float zOffset = 0f)
        {
            target.gameObject.SetActive(active);

            if (active)
            {
                target.transform.position = GetEyeDirectionPos(xOffset, yOffset, zOffset);
                target.LookAt(cameraPos, Vector3.up);
            }
        }

        /// <summary>
        /// Reverses the state of the panel
        /// </summary>
        /// <param name="target"></param>
        /// <returns>panel's current state</returns>
        private bool SwitchPanel(Transform target)
        {
            bool active = target.gameObject.activeSelf;
            SwitchPanel(target, !active, zOffset: HALF);

            return !active;
        }

        private void Start()
        {
            SwitchPanel(m_targetPanel, false);
        }

        private void Update()
        {
            // TODO: TLab.XR.Input.InputDataSource����̓��͂Ńp�l���̐؂�ւ������s
            // �ł���悤�ɂ���
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
