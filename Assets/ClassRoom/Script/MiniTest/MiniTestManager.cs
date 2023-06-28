using UnityEngine;

[System.Serializable]
public class TLabSyncMiniTestJson
{
    public int action;
    public int score;
    public int seatIndex = -1;
}

public enum WebMiniTestAction
{
    REGISTRATION,
}

public class MiniTestManager : MonoBehaviour
{
    private int[] m_scores;

    public static MiniTestManager Instance;

    public int GetScore(int index)
    {
        return m_scores[index];
    }

    public void RegistScore(int score)
    {
        m_scores[TLabSyncClient.Instalce.SeatIndex] = score;

        TLabSyncMiniTestJson obj = new TLabSyncMiniTestJson
        {
            action      = (int)WebMiniTestAction.REGISTRATION,
            score       = score,
            seatIndex   = TLabSyncClient.Instalce.SeatIndex
        };

        string json = JsonUtility.ToJson(obj);

        SendWsMessage(json, -1);
    }

    /// <summary>
    /// カスタムメッセージ受信時のコールバック処理
    /// </summary>
    /// <param name="message"></param>
    public void OnMessage(string message)
    {
        TLabSyncMiniTestJson obj = JsonUtility.FromJson<TLabSyncMiniTestJson>(message);

#if UNITY_EDITOR
        Debug.Log("[tlabsyncminitest] OnMessage - " + message);
#endif

        switch (obj.action)
        {
            case (int)WebMiniTestAction.REGISTRATION:
                m_scores[obj.seatIndex] = obj.score;
                break;
        }
    }

    /// <summary>
    /// </summary>
    /// <param name="anchorIndex">参加したプレイヤーのインデックス</param>
    public void OnGuestParticipated(int anchorIndex)
    {
        //
    }

    /// <summary>
    /// </summary>
    /// <param name="anchorIndex">退出したプレイヤーのインデックス</param>
    public void OnGuestDiscconected(int anchorIndex)
    {
        //
    }

    public void SendWsMessage(string message, int anchorIndex)
    {
        TLabSyncJson obj = new TLabSyncJson
        {
            role        = (int)WebRole.GUEST,
            action      = (int)WebAction.CUSTOMACTION,
            seatIndex   = anchorIndex,
            customIndex = 1,
            custom      = message
        };
        string json = JsonUtility.ToJson(obj);

        TLabSyncClient.Instalce.SendWsMessage(json);

        return;
    }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        m_scores = new int[TLabSyncClient.Instalce.SeatLength];
    }
}
