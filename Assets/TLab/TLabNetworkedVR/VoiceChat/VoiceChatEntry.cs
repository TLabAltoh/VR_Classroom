using System.Collections;
using UnityEngine;
using TLab.Network.VoiceChat;
using TLab.XR.VRGrabber;

public class VoiceChatEntry : MonoBehaviour
{
    [SerializeField] private VoiceChat m_voiceChat;

    private bool SocketIsOpen
    {
        get
        {
            return (SyncClient.Instance != null &&
                    SyncClient.Instance.SocketIsOpen &&
                    SyncClient.Instance.SeatIndex != -1);
        }
    }

    private IEnumerator WaitForConnection()
    {
        while (!SocketIsOpen)
        {
            yield return null;
        }

        m_voiceChat.StartVoiceChat();

        yield break;
    }

    void Start()
    {
        StartCoroutine(WaitForConnection());
    }
}
