using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TLabVoiceChat : MonoBehaviour
{
    private AudioSource m_microphoneSource;
    private AudioClip m_microphoneClip;
    private string m_microphoneName;

    void Start()
    {
        m_microphoneName = Microphone.devices[0];
        m_microphoneClip = Microphone.Start(null, false, 5, AudioSettings.outputSampleRate);

        m_microphoneSource = gameObject.AddComponent<AudioSource>();

        if (m_microphoneClip == null)
            Debug.Log("TLabVoiceChat: Failed to recording");
        else
            Debug.Log("TLabVoiceChat: Start recording");
    }

    void Update()
    {
        if (!Microphone.IsRecording(null))
        {
            m_microphoneSource.clip = m_microphoneClip;
            m_microphoneSource.Play();
            Debug.Log("Play Recording");
        }
    }
}
