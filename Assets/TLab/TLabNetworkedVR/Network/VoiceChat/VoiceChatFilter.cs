using System.Collections.Concurrent;
using UnityEngine;

namespace TLab.Network.VoiceChat
{
    public class VoiceChatFilter : MonoBehaviour
    {
        private ConcurrentQueue<float[]> m_bufferQueue = new ConcurrentQueue<float[]>();

        public void SetData(float[] data)
        {
            m_bufferQueue.Enqueue(data);
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (m_bufferQueue.Count == 0)
            {
                return;
            }

            float[] copyBuffer;
            m_bufferQueue.TryDequeue(out copyBuffer);

            int chanelSize = data.Length / channels;

            for (int i = 0; i < chanelSize; i++)
            {
                for (int j = 0; j < channels; j++)
                {
                    data[i * channels + j] = copyBuffer[i] * 20f;
                }
            }
        }
    }
}
