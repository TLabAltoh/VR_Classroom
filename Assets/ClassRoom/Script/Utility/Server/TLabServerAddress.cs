using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

[CreateAssetMenu(menuName = "TLab/ServerAddress")]
public class TLabServerAddress : ScriptableObject
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

    [SerializeField] public ServerAddress[] serverAddrArray;

    public Dictionary<AddrType, string> standardDic = new Dictionary<AddrType, string>
    {
        { AddrType.WEBSOCKET, "ws" },
        { AddrType.HTTP, "http" },
        { AddrType.HTTPS, "https" }
    };

    public bool IsMatch(AddrType type, string addr)
    {
        if (Regex.IsMatch(addr, standardDic[type] + @"://\d{1,3}(\.\d{1,3}){3}(:\d{1,7})?"))
            return true;
        else
            return false;
    }

    public void SetAddress(string name, string addr)
    {
        foreach (ServerAddress serverAddr in serverAddrArray)
            if (serverAddr.name == name)
                if (IsMatch(serverAddr.type, addr) == true)
                    serverAddr.addr = addr;
    }

    public string GetAddress(string name)
    {
        foreach(ServerAddress serverAddr in serverAddrArray)
            if (serverAddr.name == name)
                if (IsMatch(serverAddr.type, serverAddr.addr) == true)
                    return serverAddr.addr;

        return null;
    }
}
