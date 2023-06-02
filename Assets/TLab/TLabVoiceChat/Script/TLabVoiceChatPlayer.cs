using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TLabVoiceChatPlayer : MonoBehaviour
{
    private AudioSource[] m_voicePlayer = new AudioSource[2];
    private AudioClip[] m_voiceClip = new AudioClip[2];
    private bool[] m_scaduled = new bool[2];

    private int flip = 0;

    private const int PACKET_BUFFER_SIZE = VOICE_BUFFER_SIZE << SIZE_OF_FLOAT_LOG2;
    private const int VOICE_BUFFER_SIZE = 4800;
    private const int SIZE_OF_FLOAT_LOG2 = 2;
    private const int FREQUENCY = 48000;
    private const double TIME_LENGTH = (double)VOICE_BUFFER_SIZE / (double)FREQUENCY;

    private int m_lsatFlip;

    public void PlayVoice(float[] audio)
    {
        // 再生待ちの間に音をセットする
        m_voiceClip[1 - flip].SetData(audio, 0);
    }

    private void FixedUpdate()
    {
        double time = m_voicePlayer[1 - flip].time;

        // time  > 0 --> 再生が始まった
        // time == 0 --> 再生が始まるのを待っている

        if (time > 0)
        {
            m_voicePlayer[flip].PlayScheduled(AudioSettings.dspTime + (TIME_LENGTH - time));
            flip = 1 - flip;
        }
    }

    void Start()
    {
        TLabVoiceChat.Instance.RegistClient(this.gameObject.name, this);

        for (int i = 0; i < 2; i++)
        {
            GameObject child = new GameObject("Player_" + i.ToString());
            child.transform.parent = gameObject.transform;
            m_voicePlayer[i] = child.AddComponent<AudioSource>();
            m_voiceClip[i] = AudioClip.Create(this.gameObject.name + "_voiceClip", VOICE_BUFFER_SIZE, 1, FREQUENCY, false);
            m_voicePlayer[i].clip = m_voiceClip[i];

            // シームレス再生のテストに使用したコード
            //float[] data = new float[VOICE_BUFFER_SIZE];
            //for (int j = 0; j < VOICE_BUFFER_SIZE; j++)
            //    data[j] = Mathf.Sin((float)j / VOICE_BUFFER_SIZE * 500 * Mathf.PI);
            //m_voiceClip[i].SetData(data, 0);
        }

        m_voicePlayer[flip].Play();
        flip = 1 - flip;
    }
}
