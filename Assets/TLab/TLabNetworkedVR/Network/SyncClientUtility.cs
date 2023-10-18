using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TLab.XR.VRGrabber
{
    // https://kazupon.org/unity-jsonutility/#i-2
    [System.Serializable]
    public class WebVector3
    {
        public float x;
        public float y;
        public float z;
    }

    [System.Serializable]
    public class WebVector4
    {
        public float x;
        public float y;
        public float z;
        public float w;
    }

    [System.Serializable]
    public class WebObjectInfo
    {
        public string id;
        public bool rigidbody;
        public bool gravity;
        public WebVector3 position;
        public WebVector4 rotation;
        public WebVector3 scale;
    }

    [System.Serializable]
    public class WebAnimInfo
    {
        public string id;
        public string parameter;
        public int type;

        public float floatVal;
        public int intVal;
        public bool boolVal;
        public string triggerVal;
    }

    [System.Serializable]
    public class TLabSyncJson
    {
        public int role;
        public int action;

        public int seatIndex = -1;

        public bool active = false;

        public WebObjectInfo transform;
        public WebAnimInfo animator;

        public int customIndex = -1;
        public string custom = "";
    }

    public enum WebRole
    {
        SERVER,
        HOST,
        GUEST
    }

    public enum WebAction
    {
        REGIST,
        REGECT,
        ACEPT,
        EXIT,
        GUESTDISCONNECT,
        GUESTPARTICIPATION,
        ALLOCATEGRAVITY,
        REGISTRBOBJ,
        GRABBLOCK,
        FORCERELEASE,
        DIVIDEGRABBER,
        SYNCTRANSFORM,
        SYNCANIM,
        CLEARTRANSFORM,
        CLEARANIM,
        REFLESH,
        UNIREFLESHTRANSFORM,
        UNIREFLESHANIM,
        CUSTOMACTION
    }

    public enum WebAnimValueType
    {
        TYPEFLOAT,
        TYPEINT,
        TYPEBOOL,
        TYPETRIGGER
    }

    public static class SyncClientConst
    {
        // Top
        public const string COMMA = ",";
        public const string ROLE = "\"role\":";
        public const string ACTION = "\"action\":";
        public const string SEATINDEX = "\"seatIndex\":";
        public const string ACTIVE = "\"active\":";
        public const string TRANSFORM = "\"transform\":";
        public const string ANIMATOR = "\"animator\":";

        // Transform
        public const string TRANSFORM_ID = "\"id\":";
        public const string RIGIDBODY = "\"rigidbody\":";
        public const string GRAVITY = "\"gravity\":";
        public const string POSITION = "\"position\":";
        public const string ROTATION = "\"rotation\":";
        public const string SCALE = "\"scale\":";

        // Animator
        public const string ANIMATOR_ID = "\"id\":";
        public const string PARAMETER = "\"parameter\":";
        public const string TYPE = "\"type\":";

        public const string FLOATVAL = "\"floatVal\":";
        public const string INTVAL = "\"intVal\":";
        public const string BOOLVAL = "\"boolVal\":";
        public const string TRIGGERVAL = "\"triggerVal\":";

        // WebVector
        public const string X = "\"x\":";
        public const string Y = "\"y\":";
        public const string Z = "\"z\":";
        public const string W = "\"w\":";
    }
}
