namespace TLab.Network.WebRTC
{
    public enum RTCSigAction
    {
        OFFER,
        ANSWER,
        ICE,
        JOIN,
        EXIT
    }

    [System.Serializable]
    public class RTCICE
    {
        public string sdpMLineIndex;
        public string sdpMid;
        public string candidate;
    }

    [System.Serializable]
    public class RTCDesc
    {
        public int type;
        public string sdp;
    }

    [System.Serializable]
    public class RTCSigJson
    {
        public int action;
        public string room;
        public string src;
        public string dst;
        public RTCDesc desc;
        public RTCICE ice;
    }
}
