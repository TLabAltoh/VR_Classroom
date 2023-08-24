using System.Collections;
using UnityEngine;
using TLab.Network.VoiceChat;
using TLab.XR.VRGrabber;

public class VoiceChatEntry : MonoBehaviour
{
    [SerializeField] private TLabWebRTCVoiceChat m_voiceChat;

    private bool SocketIsOpen
    {
        get
        {
            return (TLabSyncClient.Instalce != null &&
                    TLabSyncClient.Instalce.SocketIsOpen == true &&
                    TLabSyncClient.Instalce.SeatIndex != -1);
        }
    }

    private IEnumerator WaitForConnection()
    {
        while (SocketIsOpen == false)
            yield return null;

        m_voiceChat.StartVoiceChat();

        yield break;
    }

    void Start()
    {
        StartCoroutine(WaitForConnection());
    }
}
