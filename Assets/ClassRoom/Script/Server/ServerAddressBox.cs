using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace TLab.VRClassroom
{
    [CreateAssetMenu(menuName = "TLab/ServerAddressBox")]
    public class ServerAddressBox : ScriptableObject
    {
        public enum AddrType
        {
            WEBSOCKET,
            HTTP,
            HTTPS
        }

        [System.Serializable]
        public class ServerAddress
        {
            public string name;
            public string addr;
            public AddrType type;
        }

        [SerializeField] public ServerAddress[] m_serverAddressList;

        public Dictionary<AddrType, string> m_standardDic = new Dictionary<AddrType, string>
        {
            { AddrType.WEBSOCKET, "ws" },
            { AddrType.HTTP, "http" },
            { AddrType.HTTPS, "https" }
        };

        public bool IsMatch(AddrType type, string addr)
        {
            if (Regex.IsMatch(addr, m_standardDic[type] + @"://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public void SetAddress(string name, string addr)
        {
            foreach (ServerAddress serverAddr in m_serverAddressList)
            {
                if (serverAddr.name == name)
                {
                    if (IsMatch(serverAddr.type, addr))
                    {
                        serverAddr.addr = addr;
                    }
                }
            }
        }

        public string GetAddress(string name)
        {
            foreach (ServerAddress serverAddr in m_serverAddressList)
            {
                if (serverAddr.name == name)
                {
                    if (IsMatch(serverAddr.type, serverAddr.addr))
                    {
                        return serverAddr.addr;
                    }
                }
            }

            return null;
        }
    }

}