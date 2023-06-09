#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;
using UnityEngine;

public class TLabAddressManager : MonoBehaviour
{
    [SerializeField] private string m_syncServerAddr = "ws://192.168.11.19:5000";
    [SerializeField] private string m_voiceChatServerAddr = "ws://192.168.11.19:5500";
    [SerializeField] private TLabSyncClient m_syncClient;
    [SerializeField] private TLabVoiceChat m_voiceChat;

    public void SetServerAddr()
    {
        if (Application.isPlaying == true) return;

        if(Regex.IsMatch(m_syncServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            if(m_syncClient != null)
                m_syncClient.SetServerAddr(m_syncServerAddr);

        if (Regex.IsMatch(m_voiceChatServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            if(m_voiceChat != null)
                m_voiceChat.SetServerAddr(m_voiceChatServerAddr);
    }
}
#endif