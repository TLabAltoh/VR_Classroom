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

    private const string thisName = "[tlabminitest] ";

    public static MiniTestManager Instance;

    public int GetScore(int index)
    {
        return m_scores[index];
    }

    public void RegistScore(int score)
    {
        m_scores[TLabSyncClient.Instalce.SeatIndex] = score;

        SendMiniTestActionMessage(
            WebMiniTestAction.REGISTRATION,
            score: score, seatIndex: TLabSyncClient.Instalce.SeatIndex, dst: -1);
    }

    public void OnMessage(string message)
    {
        TLabSyncMiniTestJson obj = JsonUtility.FromJson<TLabSyncMiniTestJson>(message);

#if UNITY_EDITOR
        Debug.Log(thisName + "OnMessage - " + message);
#endif

        switch (obj.action)
        {
            case (int)WebMiniTestAction.REGISTRATION:
                m_scores[obj.seatIndex] = obj.score;
                break;
        }
    }

    public void OnGuestParticipated(int anchorIndex) { }

    public void OnGuestDiscconected(int anchorIndex) { }

    public void SendMiniTestActionMessage(
        WebMiniTestAction action, int score = 0, int seatIndex = -1, int dst = -1)
    {
        m_scores[TLabSyncClient.Instalce.SeatIndex] = score;

        TLabSyncMiniTestJson obj = new TLabSyncMiniTestJson
        {
            action = (int)action,
            score = score,
            seatIndex = seatIndex
        };

        string customJson = JsonUtility.ToJson(obj);
        SendWsMessage(customJson, dst);
    }

    public void SendWsMessage(string customJson, int anchorIndex)
    {
        TLabSyncClient.Instalce.SendWsMessage(
            WebRole.GUEST, WebAction.CUSTOMACTION,
            seatIndex: anchorIndex, customIndex: 1, custom: customJson);

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
