using System.Text;
using UnityEngine;
using TLab.Network.WebRTC;

public class TLabWebRTCDataChannelTest : MonoBehaviour
{
    [SerializeField] TLabWebRTCDataChannel dataChannel;

    [SerializeField] private string userID;
    [SerializeField] private string roomID;

    public void Join()
    {
        dataChannel.Join(userID, roomID);
    }

    public void Exit()
    {
        dataChannel.Exit();
    }

    public void SendMessageTest(string message)
    {
        dataChannel.SendRTCMsg(Encoding.UTF8.GetBytes(message));
    }

    public void OnMessage(string dst, string src, byte[] bytes)
    {
        string receive = Encoding.UTF8.GetString(bytes);
        Debug.Log(src + " ===> " + dst + ": " + receive + ", " + "len: " + bytes.Length.ToString());
    }
}
