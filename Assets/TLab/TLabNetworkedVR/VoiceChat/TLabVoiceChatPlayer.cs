using UnityEngine;

namespace TLab.Network.VoiceChat
{
    [RequireComponent(typeof(AudioSource))]
    public class TLabVoiceChatPlayer : MonoBehaviour
    {
        [SerializeField] private bool m_useWebRTC = true;

        private AudioSource m_voicePlayer;
        private TLabVoiceChatFilter m_voiceChatFilter;

        private const int PACKET_BUFFER_SIZE = VOICE_BUFFER_SIZE << SIZE_OF_FLOAT_LOG2;
        private const int VOICE_BUFFER_SIZE = 1024;
        private const int SIZE_OF_FLOAT_LOG2 = 2;
        private const int FREQUENCY = 44100;
        private const double TIME_LENGTH = (double)VOICE_BUFFER_SIZE / (double)FREQUENCY;

        public void PlayVoice(float[] audio)
        {
            // 再生待ちの間に音をセットする
            m_voiceChatFilter.SetData(audio);
        }

        void Start()
        {
            if (m_useWebRTC == true)
                TLabWebRTCVoiceChat.Instance.RegistClient(this.gameObject.name, this);
            else
                TLabVoiceChat.Instance.RegistClient(this.gameObject.name, this);

            GameObject child = new GameObject("Player");
            child.transform.parent = this.gameObject.transform;
            m_voiceChatFilter = child.AddComponent<TLabVoiceChatFilter>();

            m_voicePlayer = child.AddComponent<AudioSource>();
            m_voicePlayer.loop = true;
            m_voicePlayer.clip = AudioClip.Create(this.gameObject.name + "_voiceClip", VOICE_BUFFER_SIZE, 1, FREQUENCY, false);

            //シームレス再生のテストに使用したコード
            float[] data = new float[VOICE_BUFFER_SIZE];
            for (int j = 0; j < VOICE_BUFFER_SIZE; j++)
                data[j] = Mathf.Sin((float)j / VOICE_BUFFER_SIZE * 15 * Mathf.PI) * 2f;
            m_voiceChatFilter.SetData(data);

            m_voicePlayer.Play();
        }
    }
}
