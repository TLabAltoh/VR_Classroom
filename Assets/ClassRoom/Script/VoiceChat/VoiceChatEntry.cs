using System.Collections;
using UnityEngine;

public class VoiceChatEntry : MonoBehaviour
{
    [SerializeField] private TLabWebRTCVoiceChat m_voiceChat;

    private IEnumerator WaitForConnection()
    {
        while (TLabSyncClient.Instalce == null ||
                TLabSyncClient.Instalce.SocketIsOpen == false ||
                TLabSyncClient.Instalce.SeatIndex == -1) yield return null;

        m_voiceChat.StartVoiceChat();

        yield break;
    }

    void Start()
    {
        StartCoroutine(WaitForConnection());
    }
}
