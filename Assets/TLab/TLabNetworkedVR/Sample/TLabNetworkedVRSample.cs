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

    private void Start()
    {

    }

    private void Update()
    {

    }
}
