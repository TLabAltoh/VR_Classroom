using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TLab.XR.VRGrabber;
using TLab.Network.VoiceChat;

public class TLabNetworkedVRSample : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] private TLabSyncClient m_syncClient;
    [SerializeField] private TLabWebRTCVoiceChat m_voiceChat;

    private IEnumerator ExitRoomTask()
    {
        // delete obj
        m_syncClient.RemoveAllGrabbers();
        m_syncClient.RemoveAllAnimators();

        yield return null;
        yield return null;

        // close socket
        m_voiceChat.CloseRTC();
        m_syncClient.CloseRTC();

        yield return null;
        yield return null;

        m_syncClient.Exit();

        yield return null;
        yield return null;

        float remain = 1.5f;
        while (remain > 0)
        {
            remain -= Time.deltaTime;
            yield return null;
        }
    }

    private void Start()
    {

    }

    private void Update()
    {

    }
}
