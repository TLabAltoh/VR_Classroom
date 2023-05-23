using System;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TLabVoiceChat : MonoBehaviour
{
    [SerializeField] private LineRenderer m_lineRenderer;
    [SerializeField] private float m_waveLength = 20.0f;
    [SerializeField] private float m_yLength = 10f;
    [SerializeField] public bool m_loopBackSelf = false;
    [SerializeField] public bool m_isStreaming = false;

    private AudioSource m_microphoneSource;
    private AudioClip m_microphoneClip;
    private string m_microphoneName;

    private float[] m_data = default;
    private int m_sampleStep = default;
    private Vector3[] m_samplingLinePoints = default;

    private int m_writeHead;
    private int m_readHead;

    POTBuf[] potBuffers = new POTBuf[POTBuf.POT_max + 1];

    public delegate void MicCallbackDelegate(float[] buf);
    public MicCallbackDelegate floatsInDelegate;

    private class POTBuf
    {
        // 2^6 = 64
        // 2^7 = 128
        // 2^8 = 256
        // 2^9 = 512
        // 2^10 = 1024

        public const int POT_min = 6;
        public const int POT_max = 10;

        const int redundancy = 8;
        int index = 0;

        float[][] internalBuffers = new float[redundancy][];

        public float[] buf
        {
            get
            {
                return internalBuffers[index];
            }
        }

        public void Cycle()
        {
            index = (index + 1) % redundancy;
        }

        public POTBuf(int POT)
        {
            for (int r = 0; r < redundancy; r++)
            {
                internalBuffers[r] = new float[1 << POT];
            }
        }
    }

    private void Render(Vector3[] points)
    {
        if (points == null) return;
        m_lineRenderer.positionCount = points.Length;
        m_lineRenderer.SetPositions(points);
    }

    private void FixedUpdate()
    {
        m_writeHead = Microphone.GetPosition(m_microphoneName);

        if (m_readHead == m_writeHead || potBuffers == null || !m_isStreaming)
            return;

        // Say audio.clip.samples (S)  = 100
        // if w=1, r=0, we want 1 sample.  ( S + 1 - 0 ) % S = 1 YES
        // if w=0, r=99, we want 1 sample.  ( S + 0 - 99 ) % S = 1 YES
        int nFloatsToGet = (m_microphoneClip.samples + m_writeHead - m_readHead) % m_microphoneClip.samples;

        for (int k = POTBuf.POT_max; k >= POTBuf.POT_min; k--)
        {
            POTBuf B = potBuffers[k];

            int n = B.buf.Length; // i.e.  1 << k;

            while (nFloatsToGet >= n)
            {

                // If the read length from the offset is longer than the clip length,
                //   the read will wrap around and read the remaining samples
                //   from the start of the clip.
                m_microphoneClip.GetData(B.buf, m_readHead);
                m_readHead = (m_readHead + n) % m_microphoneClip.samples;

                if (floatsInDelegate != null)
                    floatsInDelegate(B.buf);

                B.Cycle();
                nFloatsToGet -= n;
            }
        }

        if (m_microphoneSource.isPlaying)
        {
            var startIndex = m_microphoneSource.timeSamples;
            var endIndex = m_microphoneSource.timeSamples + m_sampleStep;

            var samples = Math.Max(endIndex - startIndex, 1f);
            var xStep = m_waveLength / samples;
            var j = 0;

            for (var i = startIndex; i < endIndex; i++, j++)
            {
                var x = (-m_waveLength / 2f) + xStep * j;
                var y = i < m_data.Length ? m_data[i] * m_yLength : 0f;
                var p = new Vector3(x, y, 0) + this.transform.position;
                m_samplingLinePoints[j] = p;
            }

            Render(m_samplingLinePoints);
        }
        else
            Reset();
    }

    private void Reset()
    {
        var x = -m_waveLength / 2;
        Render(new[]
        {
            new Vector3(-x, 0, 0) + this.transform.position,
            this.transform.position,
            new Vector3(x, 0, 0) + this.transform.position,
        });
    }

    void Start()
    {
        m_microphoneSource = GetComponent<AudioSource>();

        string deviceList = "Currently connected device:\n";
        foreach (string deveice in Microphone.devices)
            deviceList += "\t" + deveice + "\n";
        Debug.Log(deviceList);

        //m_microphoneName = Microphone.devices[0];
        m_microphoneName = "エコー キャンセル スピーカーフォン (Jabra Speak 710)";

        m_microphoneClip = Microphone.Start(m_microphoneName, true, 1, AudioSettings.outputSampleRate);

        if (m_microphoneClip == null)
            Debug.Log("TLabVoiceChat: Failed to recording, using " + m_microphoneName);
        else
            Debug.Log("TLabVoiceChat: Start recording, using " + m_microphoneName);

        m_microphoneSource.clip = m_microphoneClip;
        m_microphoneSource.loop = true;

        if (m_loopBackSelf)
        {
            while (!(Microphone.GetPosition(m_microphoneName) > 0)) { }
            m_microphoneSource.Play();

            Debug.Log("TLabVoiceChat: Sart Loop Back");
        }

        m_data = new float[m_microphoneClip.channels * m_microphoneClip.samples];
        m_microphoneSource.clip.GetData(m_data, 0);

        m_sampleStep = (int)(m_microphoneClip.frequency / Mathf.Max(60f, 1f / Time.fixedDeltaTime));
        m_samplingLinePoints = new Vector3[m_sampleStep];
    }
}
