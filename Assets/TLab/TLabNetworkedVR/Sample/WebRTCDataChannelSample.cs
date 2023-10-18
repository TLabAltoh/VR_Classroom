using System.Text;
using UnityEngine;
using TLab.Network.WebRTC;

[ExecuteAlways] [RequireComponent(typeof(WebRTCDataChannel))]
public class WebRTCDataChannelSample : MonoBehaviour
{
    [SerializeField] private WebRTCDataChannel m_dataChannel;

    [SerializeField] private string m_userID;
    [SerializeField] private string m_roomID;

    void Reset()
    {
        if(m_dataChannel == null)
        {
            m_dataChannel = GetComponent<WebRTCDataChannel>();
        }
    }

    public void Join()
    {
        m_dataChannel.Join(m_userID, m_roomID);
    }

    public void Exit()
    {
        m_dataChannel.Exit();
    }

    public void SendMessageTest(string message)
    {
        m_dataChannel.SendRTCMsg(Encoding.UTF8.GetBytes(message));
    }

    public void OnMessage(string dst, string src, byte[] bytes)
    {
        string receive = Encoding.UTF8.GetString(bytes);
        Debug.Log(src + " ===> " + dst + ": " + receive + ", " + "len: " + bytes.Length.ToString());
    }
}
