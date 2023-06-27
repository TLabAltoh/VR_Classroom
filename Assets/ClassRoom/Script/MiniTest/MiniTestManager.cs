using System.Collections;
using System.Collections.Generic;
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
    registration,
    aggregateResults,
}

public class MiniTestManager : MonoBehaviour
{
    private int[] scores;

    public void SendWsMessage(string message, int anchorIndex)
    {
        TLabSyncJson obj = new TLabSyncJson
        {
            role = (int)WebRole.guest,
            action = (int)WebAction.customAction,
            seatIndex = anchorIndex,
            customIndex = 1,
            custom = message
        };
        string json = JsonUtility.ToJson(obj);

        TLabSyncClient.Instalce.SendWsMessage(json);

        return;
    }

    /// <summary>
    /// �J�X�^�����b�Z�[�W��M���̃R�[���o�b�N����
    /// </summary>
    /// <param name="message"></param>
    public void OnMessage(string message)
    {
        TLabSyncMiniTestJson obj = JsonUtility.FromJson<TLabSyncMiniTestJson>(message);

#if UNITY_EDITOR
        Debug.Log("[tlabsyncminitest] OnMessage - " + message);
#endif

        if (obj.action == (int)WebMiniTestAction.registration)
        {

        }
        else if (obj.action == (int)WebMiniTestAction.aggregateResults)
        {

        }

        return;
    }

    /// <summary>
    /// </summary>
    /// <param name="anchorIndex">�Q�������v���C���[�̃C���f�b�N�X</param>
    public void OnGuestParticipated(int anchorIndex)
    {
        //
    }

    /// <summary>
    /// </summary>
    /// <param name="anchorIndex">�ޏo�����v���C���[�̃C���f�b�N�X</param>
    public void OnGuestDiscconected(int anchorIndex)
    {
        //
    }
}
