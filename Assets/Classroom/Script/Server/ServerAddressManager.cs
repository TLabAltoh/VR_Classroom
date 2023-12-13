using UnityEngine;
using UnityEditor;
using TLab.XR.Network;
using TLab.Network.WebRTC;

namespace TLab.VRClassroom
{
    public class ServerAddressManager : MonoBehaviour
    {
        [SerializeField] private ServerAddressBox m_serverAddressBox;

        [SerializeField] private SyncClient m_syncClient;
        [SerializeField] private WebRTCDataChannel m_voiceChat;
        [SerializeField] private WebRTCDataChannel m_syncTransform;

        private string THIS_NAME => "[" + this.GetType().Name + "] ";

        private void RegistSyncServerAddress()
        {
            string addr = m_serverAddressBox.GetAddress(ClassroomEntry.SYNC_SERVER)?.addr;

            if (addr == null)
            {
                return;
            }

            Debug.Log(THIS_NAME + "set sync server address: " + addr);

            m_syncClient?.SetServerAddr(addr);

#if UNITY_EDITOR
            if(m_syncClient != null)
            {
                EditorUtility.SetDirty(m_syncClient);
            }
#endif
        }

        private void RegistSignalingServerAddress()
        {
            string addr = m_serverAddressBox.GetAddress(ClassroomEntry.SIGNALING_SERVER)?.addr;

            if (addr == null)
            {
                return;
            }

            Debug.Log(THIS_NAME + "set signaling address: " + addr);

            m_voiceChat?.SetSignalingServerAddr(addr);
            m_syncTransform?.SetSignalingServerAddr(addr);

#if UNITY_EDITOR
            if(m_voiceChat != null)
            {
                EditorUtility.SetDirty(m_voiceChat);
            }
            
            if(m_syncTransform != null)
            {
                EditorUtility.SetDirty(m_syncTransform);
            }
#endif
        }

        public void RegistServerAddress()
        {
            RegistSyncServerAddress();
            RegistSignalingServerAddress();
        }

        void Awake()
        {
            RegistServerAddress();
        }
    }
}