#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TLabAddressManager : MonoBehaviour
{
    [SerializeField] private string m_syncServerAddr        = "ws://192.168.11.19:5000";
    [SerializeField] private string m_voiceChatServerAddr   = "ws://192.168.11.19:5500";
    [SerializeField] private string m_shelfServerAddr       = "http://192.168.3.19:5600/StandaloneWindows/testmodel.assetbundl";
    [SerializeField] private TLabSyncClient m_syncClient;
    [SerializeField] private TLabVoiceChat m_voiceChat;
    [SerializeField] private TLabShelfSyncManager m_shelfManager;

    private string m_syncServerLastAddr = "";
    private string m_voiceChatServerLastAddr = "";
    private string m_shelfServerLastAddr = "";

    public string SyncServerAddr
    {
        get
        {
            if (Regex.IsMatch(m_syncServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
                return m_syncServerAddr;
            else
                return null;
        }
    }

    public string VoiceChatAddr
    {
        get
        {
            if (Regex.IsMatch(m_voiceChatServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
                return m_voiceChatServerAddr;
            else
                return null;
        }
    }

    public string ShelfServerAddr
    {
        get
        {
            if (Regex.IsMatch(m_shelfServerAddr, @"http://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
                return m_shelfServerAddr;
            else
                return null;
        }
    }

    public void SetServerAddr()
    {
        if (Regex.IsMatch(m_syncServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            if (m_syncClient != null && m_syncServerAddr != m_syncServerLastAddr)
            {
                m_syncClient.SetServerAddr(m_syncServerAddr);
                EditorUtility.SetDirty(m_syncClient);
                m_syncServerLastAddr = m_syncServerAddr;
            }

        if (Regex.IsMatch(m_voiceChatServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            if (m_voiceChat != null && m_voiceChatServerAddr != m_voiceChatServerLastAddr)
            {
                m_voiceChat.SetServerAddr(m_voiceChatServerAddr);
                EditorUtility.SetDirty(m_voiceChat);
                m_voiceChatServerLastAddr = m_voiceChatServerAddr;
            }

        if (Regex.IsMatch(m_shelfServerAddr, @"http://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            if (m_shelfManager != null && m_shelfServerAddr != m_shelfServerLastAddr)
            {
                m_shelfManager.SetServerAddr(m_shelfServerAddr);
                EditorUtility.SetDirty(m_shelfManager);
                m_shelfServerLastAddr = m_shelfServerAddr;
            }
    }
}
#endif

#if UNITY_EDITOR
[CustomEditor(typeof(TLabAddressManager))]
[CanEditMultipleObjects]
public class TLabAddressManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        serializedObject.Update();

        if (GUILayout.Button("Set Server Addr"))
        {
            TLabAddressManager manager = target as TLabAddressManager;
            manager.SetServerAddr();
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif