using System.Collections;
using UnityEngine;
using TLab.XR.Network;
using TLab.Network.VoiceChat;

public class VoiceChatEntry : MonoBehaviour
{
    [SerializeField] private VoiceChat m_voiceChat;

    private bool socketIsOpen
    {
        get
        {
            return (SyncClient.Instance != null &&
                    SyncClient.Instance.socketIsOpen &&
                    SyncClient.Instance.seatIndex != SyncClient.NOT_REGISTED);
        }
    }

    private IEnumerator WaitForConnection()
    {
        while (!socketIsOpen)
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
