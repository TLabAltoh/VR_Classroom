using System.Collections;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TLabVoiceChatPlayer : MonoBehaviour
{
    private AudioSource m_voicePlayer;
    private AudioClip m_voiceClip;

    private const int PACKET_BUFFER_SIZE = VOICE_BUFFER_SIZE << SIZE_OF_FLOAT_LOG2;
    private const int VOICE_BUFFER_SIZE = 1024;
    private const int SIZE_OF_FLOAT_LOG2 = 2;

    IEnumerator PlayVoiceTask(float[] audio)
    {
        while (m_voicePlayer.isPlaying == true)
            yield return null;

        m_voiceClip.SetData(audio, 0);
        m_voicePlayer.Play();
    }

    public void PlayVoice(float[] audio)
    {
        StartCoroutine(PlayVoiceTask(audio));

        //m_voiceClip.SetData(audio, 0);
        //m_voicePlayer.Play();
    }

    void Start()
    {
        TLabVoiceChat.Instance.RegistClient(this.gameObject.name, this);

        m_voicePlayer = GetComponent<AudioSource>();
        m_voiceClip = AudioClip.Create(this.gameObject.name + "_voiceClip", VOICE_BUFFER_SIZE, 1, 48000, false);

        m_voicePlayer.clip = m_voiceClip;
    }
}
