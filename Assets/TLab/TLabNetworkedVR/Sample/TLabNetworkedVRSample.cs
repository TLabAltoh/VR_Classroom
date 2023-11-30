using System.Collections;
using UnityEngine;
using TLab.XR.Interact;
using TLab.XR.Network;
using TLab.Network.VoiceChat;

public class TLabNetworkedVRSample : MonoBehaviour
{
    [Header("Network")]
    [SerializeField] private SyncClient m_syncClient;
    [SerializeField] private VoiceChat m_voiceChat;

    private IEnumerator ExitRoomTask()
    {
        // delete obj

        Grabbable.ClearRegistry();
        SyncAnimator.ClearRegistry();

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
