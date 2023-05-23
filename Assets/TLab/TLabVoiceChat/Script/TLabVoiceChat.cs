using NativeWebSocket;
using UnityEngine;

[System.Serializable]
public class TLabVoiceJson
{
    public string audio;
}

[RequireComponent(typeof(AudioSource))]
public class TLabVoiceChat : MonoBehaviour
{
    [SerializeField] public bool m_loopBackSelf = false;
    [SerializeField] public bool m_isStreaming = false;

    private AudioSource m_microphoneSource;
    private AudioClip m_microphoneClip;
    private string m_microphoneName;

    private int m_writeHead;
    private int m_readHead;

    POTBuf[] potBuffers = new POTBuf[POTBuf.POT_max + 1];

    public delegate void MicCallbackDelegate(float[] buf);
    public MicCallbackDelegate floatsInDelegate;

    private float[] m_packetBuffer = new float[PACKET_BUFFER_SIZE];
    private int m_pbWriteHead = 0;
    private const int PACKET_BUFFER_SIZE = 1024;
    private const int SIZE_OF_FLOAT = 32;

    /*
     * Obtain microphone input in real time
     * https://forum.unity.com/threads/real-time-audio-from-microphone.145686/
     */

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

    private void SetupBuffers()
    {
        for (int k = POTBuf.POT_min; k <= POTBuf.POT_max; k++)
            potBuffers[k] = new POTBuf(k);
    }

    private unsafe void LongCopy(byte* src, byte* dst, int count)
    {
        // https://github.com/neuecc/MessagePack-CSharp/issues/117
        // Define it as an internal function in the thread to avoid method brute force.

        while (count >= 8)
        {
            *(ulong*)dst = *(ulong*)src;
            dst += 8;
            src += 8;
            count -= 8;
        }
        if (count >= 4)
        {
            *(uint*)dst = *(uint*)src;
            dst += 4;
            src += 4;
            count -= 4;
        }
        if (count >= 2)
        {
            *(ushort*)dst = *(ushort*)src;
            dst += 2;
            src += 2;
            count -= 2;
        }
        if (count >= 1)
        {
            *dst = *src;
        }
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

            // 1 << k;
            int n = B.buf.Length;

            while (nFloatsToGet >= n)
            {
                m_microphoneClip.GetData(B.buf, m_readHead);
                m_readHead = (m_readHead + n) % m_microphoneClip.samples;

                if (floatsInDelegate != null)
                    floatsInDelegate(B.buf);

                B.Cycle();
                nFloatsToGet -= n;
            }
        }
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
            Debug.Log("TLabVoiceChat: Start recording, using " + m_microphoneName + ", samples: " + m_microphoneClip.samples + ", channels: " + m_microphoneClip.channels);

        if (m_loopBackSelf)
        {
            while (!(Microphone.GetPosition(m_microphoneName) > 0)) { }
            m_microphoneSource.clip = m_microphoneClip;
            m_microphoneSource.loop = true;
            m_microphoneSource.Play();

            Debug.Log("TLabVoiceChat: Sart Loop Back");
        }

        SetupBuffers();

        floatsInDelegate += (float[] buffer) =>
        {
            int sum = m_pbWriteHead + buffer.Length;

            if (sum > PACKET_BUFFER_SIZE)
            {
                unsafe
                {
                    int size = PACKET_BUFFER_SIZE - m_pbWriteHead;
                    fixed(float* root = &(m_packetBuffer[0]), src = &buffer[0])
                    {
                        LongCopy((byte*)(src + m_pbWriteHead), (byte*)(root + m_pbWriteHead), size * sizeof(float));

                        // transmission process
                        string encBuffer = System.Text.Encoding.UTF8.GetString((byte*)root, PACKET_BUFFER_SIZE * sizeof(float));
                        Debug.Log("Send Audio Packet 0");

                        m_pbWriteHead = (m_pbWriteHead + size) % PACKET_BUFFER_SIZE;
                    }
                    size = buffer.Length - size;
                    fixed (float* dst = &(m_packetBuffer[m_pbWriteHead]), src = &buffer[size])
                    {
                        LongCopy((byte*)src, (byte*)dst, size * sizeof(float));
                        m_pbWriteHead = (m_pbWriteHead + size) % PACKET_BUFFER_SIZE;
                    }
                }
            }
            else if (sum == PACKET_BUFFER_SIZE)
            {
                unsafe
                {
                    fixed (float* root = &(m_packetBuffer[0]), src = &buffer[0])
                    {
                        LongCopy((byte*)(src + m_pbWriteHead), (byte*)(root + m_pbWriteHead), buffer.Length * sizeof(float));

                        // transmission process
                        string encBuffer = System.Text.Encoding.UTF8.GetString((byte*)root, PACKET_BUFFER_SIZE * sizeof(float));
                        Debug.Log("Send Audio Packet 1");

                        m_pbWriteHead = (m_pbWriteHead + buffer.Length) % PACKET_BUFFER_SIZE;
                    }
                }
            }
            else
            {
                unsafe
                {
                    fixed (float* dst = &(m_packetBuffer[m_pbWriteHead]), src = &buffer[0])
                    {
                        LongCopy((byte*)src, (byte*)dst, buffer.Length * sizeof(float));
                        m_pbWriteHead = (m_pbWriteHead + buffer.Length) % PACKET_BUFFER_SIZE;
                    }
                }
            }
        };
    }
}
