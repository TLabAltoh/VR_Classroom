using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction;
using TLab.InputField;
using TLab.XR.Network;
using TLab.Network.WebRTC.Voice;

namespace TLab.VRClassroom
{
    // 目標はTLab.XR.Input.InputDataSourceのみが，プラグインに依存するソースコードにしたい

    public class Classroom : MonoBehaviour
    {
        [Header("Menu Panel")]
        [SerializeField] private Transform m_centerEyeAnchor;
        [SerializeField] private Transform m_targetPanel;
        [SerializeField] private Transform m_webViewPanel;

        // TODO: RayInteractableを参照しない方法を検討する
        [Header("Keyborad")]
        [SerializeField] private TLabVKeyborad m_keyborad;
        [SerializeField] private RayInteractable m_keyboradInteractable;

        [Header("Network")]
        [SerializeField] private SyncClient m_syncClient;
        [SerializeField] private VoiceChat m_voiceChat;

        private const float HALF = 0.5f;

        public static string ENTRY_SCENE = "ENTRY";
        public static string HOST_SCENE = "HOST";
        public static string GUEST_SCENE = "GUEST";
        public static string DEMO_SCENE = "DEMO_SCENE";

        private Vector3 cameraPos => Camera.main.transform.position;

        private IEnumerator ExitClassroomTask()
        {
            // clear networked objct
            SyncTransformer.ClearRegistry();
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
            string scene = SyncClient.Instance.isHost ? HOST_SCENE : GUEST_SCENE;

            yield return ExitClassroomTask();

            SceneManager.LoadSceneAsync(scene, LoadSceneMode.Single);

            yield break;
        }

        private IEnumerator BackToTheEntryTask()
        {
            yield return ExitClassroomTask();

            SceneManager.LoadSceneAsync(ENTRY_SCENE, LoadSceneMode.Single);

            yield break;
        }

        public void ReEnter() => StartCoroutine(ReEnterClassroomTask());

        public void ExitClassroom() => StartCoroutine(BackToTheEntryTask());

        public void ShowWebView()
        {
            bool active = SwitchPanel(m_webViewPanel);

            m_keyborad.HideKeyborad(!active);
        }

        private Vector3 GetEyeDirectionPos(float xOffset = 0f, float yOffset = 0f, float zOffset = 0f)
        {
            return m_centerEyeAnchor.position +
                    m_centerEyeAnchor.right * xOffset +
                    m_centerEyeAnchor.up * yOffset +
                    m_centerEyeAnchor.forward * zOffset;
        }

        /// <summary>
        /// TLabVKeyborad.onHideで呼び出すコールバック
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
                // パネルよりも少し下に表示
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
        private void SwitchPanel(Transform target, bool active, Vector3 offset)
        {
            target.gameObject.SetActive(active);

            if (active)
            {
                target.transform.position = GetEyeDirectionPos(xOffset: offset.x, yOffset: offset.y, zOffset: offset.z);
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
            SwitchPanel(target, !active, new Vector3(0f, 0f, HALF));

            return !active;
        }

        private void Start()
        {
            m_keyborad.HideKeyborad(true);
            SwitchPanel(m_targetPanel, false, Vector3.zero);
        }

        private void Update()
        {
            // TODO: TLab.XR.Input.InputDataSourceからの入力でパネルの切り替えを実行
            // できるようにする
            if (OVRInput.GetDown(OVRInput.Button.Start))
            {
                if (!SwitchPanel(m_targetPanel))
                {
                    SwitchPanel(m_webViewPanel, false, Vector3.zero);

                    // Guest側には講義資料しか用意していない
                    if (!SyncClient.Instance.isHost)
                    {
                        m_keyborad.HideKeyborad(true);
                    }
                }
            }
        }
    }
}
