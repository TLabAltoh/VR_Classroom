using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using TLab.XR.VRGrabber;
using TLab.Network.WebRTC;

namespace TLab.VRClassroom
{
    public class ServerAddressManager : MonoBehaviour
    {
        [SerializeField] private ServerAddressBox m_serverAddressBox;

        [SerializeField] private SyncClient m_syncClient;
        [SerializeField] private SyncShelfManager m_shelfManager;
        [SerializeField] private WebRTCDataChannel m_voiceChat;
        [SerializeField] private WebRTCDataChannel m_syncTransform;

        private string THIS_NAME = "[addressmanager] ";

        private void SetSyncServerAddr()
        {
            string addr = m_serverAddressBox.GetAddress("SyncServer");

            if (addr == null)
            {
                return;
            }

            Debug.Log(THIS_NAME + addr);

            m_syncClient.SetServerAddr(addr);

#if UNITY_EDITOR
            EditorUtility.SetDirty(m_syncClient);
#endif
        }

        private void SetShelfServerAddr()
        {
            string addr = m_serverAddressBox.GetAddress("Shelf");

            if (addr == null)
            {
                return;
            }

            Debug.Log(THIS_NAME + addr);

            m_shelfManager.SetServerAddr(addr);

#if UNITY_EDITOR
            EditorUtility.SetDirty(m_shelfManager);
#endif
        }

        private void SetSignalingServerAddr()
        {
            string addr = m_serverAddressBox.GetAddress("Signaling");

            if (addr == null)
            {
                return;
            }

            Debug.Log(THIS_NAME + addr);

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
    }
}