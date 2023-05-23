using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TLabVoiceChatPlayer : MonoBehaviour
{
    private AudioSource m_voicePlayer;
    private AudioClip m_voiceClip;

    public void PlayVoice(float[] audio)
    {

    }

    void Start()
    {
        m_voicePlayer = GetComponent<AudioSource>();
        m_voiceClip = AudioClip.Create(this.gameObject.name + "_voiceClip", 1, 1, AudioSettings.outputSampleRate, true);
    }
}
