using System.Text;
using UnityEngine;

public class TLabUIUtil : MonoBehaviour
{
    [SerializeField] private TLabUIAnimState[] m_states;

    private StringBuilder builder = new StringBuilder();

    public void SetCursor(int index)
    {
        //
        // Set anim state
        //

        TLabUIAnimState target = m_states[index];

        if (target.animator.GetCurrentAnimatorStateInfo(0).IsTag("CursorOff") == true)
            target.cursorOn = true;
        else
            target.cursorOn = false;

        target.animator.SetBool("CursorOn", target.cursorOn);

        //
        // Sync Anim state
        //

        // Optimized to reduce the amount of data

        // Use StringBuilder for speed optimization
        // https://qiita.com/TD12734/items/fad83dddb8f0452b7d38

        builder.Clear();

        builder.Append("{");

            builder.Append(TLabSyncClientConst.ROLE);
            builder.Append(((int)WebRole.guest).ToString());
            builder.Append(TLabSyncClientConst.COMMA);

            builder.Append(TLabSyncClientConst.ACTION);
            builder.Append(((int)WebAction.syncAnim).ToString());
            builder.Append(TLabSyncClientConst.COMMA);

            builder.Append(TLabSyncClientConst.ANIMATOR);
            builder.Append("{");
                builder.Append(TLabSyncClientConst.ANIMATOR_ID);
                builder.Append("\"");
                builder.Append(target.animator.gameObject.name);
                builder.Append("\"");
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.PARAMETER);
                builder.Append("\"CursorOn\"");
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.TYPE);
                builder.Append(((int)WebAnimValueType.typeBool).ToString());
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.FLOATVAL);
                builder.Append(0.0f);
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.INTVAL);
                builder.Append(0);
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.BOOLVAL);
                builder.Append((target.cursorOn ? "true" : "false"));
                builder.Append(TLabSyncClientConst.COMMA);

                builder.Append(TLabSyncClientConst.TRIGGERVAL);
                builder.Append("\"\"");
            builder.Append("}");
        builder.Append("}");

        string json = builder.ToString();

        #region Packet to make
        //TLabSyncJson obj = new TLabSyncJson
        //{
        //    role = (int)WebRole.guest,
        //    action = (int)WebAction.syncAnim,
        //    animator = new WebAnimInfo
        //    {
        //        id = target.animator.gameObject.name,
        //        parameter = "CursorOn",
        //        type = (int)WebAnimValueType.typeBool,
        //        boolVal = target.cursorOn
        //    }
        //};

        //string json = JsonUtility.ToJson(obj);
        #endregion

        TLabSyncClient.Instalce.SendWsMessage(json);
    }
}

[System.Serializable]
public class TLabUIAnimState
{
    [System.NonSerialized] public bool cursorOn = false;
    public Animator animator;
}
