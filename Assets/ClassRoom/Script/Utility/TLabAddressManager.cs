#if UNITY_EDITOR
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

public class TLabAddressManager : MonoBehaviour
{
    [SerializeField] private string m_syncServerAddr = "ws://192.168.11.19:5000";
    [SerializeField] private string m_signalingServerAddr = "ws://192.168.11.19:3001";
    [SerializeField] private string m_shelfServerAddr = "http://192.168.3.19:5600/StandaloneWindows/testmodel.assetbundl";
    [SerializeField] private TLabSyncClient m_syncClient;
    [SerializeField] private TLabWebRTCDataChannel m_voiceChat;
    [SerializeField] private TLabWebRTCDataChannel m_syncTransform;
    [SerializeField] private TLabShelfSyncManager m_shelfManager;

    private string m_syncServerLastAddr = "";
    private string m_signalingServerLastAddr = "";
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

    public string SignalingServerAddr
    {
        get
        {
            if (Regex.IsMatch(m_signalingServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
                return m_signalingServerAddr;
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

    private void SetSyncServerAddr()
    {
        if (Regex.IsMatch(m_syncServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            if (m_syncClient != null && m_syncServerAddr != m_syncServerLastAddr)
            {
                m_syncClient.SetServerAddr(m_syncServerAddr);
                EditorUtility.SetDirty(m_syncClient);
                m_syncServerLastAddr = m_syncServerAddr;
            }
    }

    private void SetShelfServerAddr()
    {
        if (Regex.IsMatch(m_shelfServerAddr, @"http://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            if (m_shelfManager != null && m_shelfServerAddr != m_shelfServerLastAddr)
            {
                m_shelfManager.SetServerAddr(m_shelfServerAddr);
                EditorUtility.SetDirty(m_shelfManager);
                m_shelfServerLastAddr = m_shelfServerAddr;
            }
    }

    private void SetSignalingServerAddr()
    {
        if (Regex.IsMatch(m_signalingServerAddr, @"ws://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            if (m_voiceChat != null && m_signalingServerAddr != m_signalingServerLastAddr)
            {
                m_voiceChat.SetSignalingServerAddr(m_signalingServerAddr);
                m_syncTransform.SetSignalingServerAddr(m_signalingServerAddr);
                EditorUtility.SetDirty(m_voiceChat);
                EditorUtility.SetDirty(m_syncTransform);
                m_signalingServerLastAddr = m_signalingServerAddr;
            }
    }

    public void SetServerAddr()
    {
        SetSyncServerAddr();
        SetShelfServerAddr();
        SetSignalingServerAddr();
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