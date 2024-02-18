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
        [SerializeField] private WebRTCClient m_webrtcClient;

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
            if (m_syncClient != null)
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

            Debug.Log(THIS_NAME + "Set Signaling Address: " + addr);

            m_webrtcClient?.SetSignalingServerAddr(addr);

#if UNITY_EDITOR
            if (m_webrtcClient != null)
            {
                EditorUtility.SetDirty(m_webrtcClient);
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