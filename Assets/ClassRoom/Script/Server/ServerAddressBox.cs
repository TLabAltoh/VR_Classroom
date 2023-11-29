using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TLab.VRClassroom
{
    [CreateAssetMenu(menuName = "TLab/ServerAddressBox")]
    public class ServerAddressBox : ScriptableObject
    {
        public enum Protocol
        {
            WEBSOCKET,
            HTTP,
            HTTPS
        }

        public static Dictionary<Protocol, string> PROTOCOL = new Dictionary<Protocol, string>
        {
            { Protocol.WEBSOCKET, "ws" },
            { Protocol.HTTP, "http" },
            { Protocol.HTTPS, "https" }
        };

        [System.Serializable]
        public class ServerAddress
        {
            public string name;
            public string addr;
            public Protocol protocol;
        }

        [SerializeField] private ServerAddress[] m_serverAddressList;

        public bool IsMatch(Protocol type, string addr)
        {
            if (Regex.IsMatch(addr, PROTOCOL[type] + @"://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public ServerAddress GetAddress(string name)
        {
            foreach (ServerAddress serverAddr in m_serverAddressList)
            {
                if (serverAddr.name == name)
                {
                    return serverAddr;
                }
            }

            return null;
        }

        public void SetAddress(string name, string addr, string port = null)
        {
            var serverAddr = GetAddress(name);
            if(serverAddr != null)
            {
                string newAddr = PROTOCOL[serverAddr.protocol] + "://" + addr;

                if(port != null)
                {
                    newAddr += ":" + port;
                }

                serverAddr.addr = newAddr;
            }
        }
    }

}