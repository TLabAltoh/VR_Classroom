using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using TLab.XR.VRGrabber;
using TLab.Network.WebRTC;

public class TLabAddressManager : MonoBehaviour
{
    [SerializeField] TLabServerAddress m_serverAddrs;

    [SerializeField] private TLabSyncClient m_syncClient;
    [SerializeField] private TLabShelfSyncManager m_shelfManager;
    [SerializeField] private TLabWebRTCDataChannel m_voiceChat;
    [SerializeField] private TLabWebRTCDataChannel m_syncTransform;

    private string thisName = "[addressmanager] ";

    private void SetSyncServerAddr()
    {
        string addr = m_serverAddrs.GetAddress("SyncServer");
        if (addr == null) return;

        Debug.Log(thisName + addr);

        m_syncClient.SetServerAddr(addr);

#if UNITY_EDITOR
        EditorUtility.SetDirty(m_syncClient);
#endif
    }

    private void SetShelfServerAddr()
    {
        string addr = m_serverAddrs.GetAddress("Shelf");
        if (addr == null) return;

        Debug.Log(thisName + addr);

        m_shelfManager.SetServerAddr(addr);

#if UNITY_EDITOR
        EditorUtility.SetDirty(m_shelfManager);
#endif
    }

    private void SetSignalingServerAddr()
    {
        string addr = m_serverAddrs.GetAddress("Signaling");
        if (addr == null) return;

        Debug.Log(thisName + addr);

        m_voiceChat.SetSignalingServerAddr(addr);
        m_syncTransform.SetSignalingServerAddr(addr);

#if UNITY_EDITOR
        EditorUtility.SetDirty(m_voiceChat);
        EditorUtility.SetDirty(m_syncTransform);
#endif
    }

    public void SetServerAddr()
    {
        SetSyncServerAddr();
        SetShelfServerAddr();
        SetSignalingServerAddr();
    }

    private void Awake()
    {
        SetServerAddr();
    }
}

#if UNITY_EDITOR
[CanEditMultipleObjects]
[CustomEditor(typeof(TLabAddressManager))]
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