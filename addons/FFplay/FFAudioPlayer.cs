using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Godot;

namespace FFmpeg.Godot
{
    [GlobalClass]
    public partial class FFAudioPlayer : Node
    {
        public long pts;

        [Export]
        public AudioStreamPlayer audioSource;

        private AudioStreamGenerator clip;

        private int channels;

        private AVSampleFormat sampleFormat;

        private readonly List<float> pcm = [];

        private AudioStreamGeneratorPlayback playback;

        public void Init(int frequency, int channels, AVSampleFormat sampleFormat)
        {
            this.channels = channels;

            this.sampleFormat = sampleFormat;

            GD.Print($"Freq={frequency}");

            clip = new AudioStreamGenerator()
            {
                BufferLength = 1f,
                MixRate = frequency,
            };

            audioSource.Stream = clip;

            audioSource.Play();

            if (audioSource.GetStreamPlayback() is AudioStreamGeneratorPlayback pb)
                playback = pb;
        }

        public void Pause()
        {
            audioSource.StreamPaused = true;
        }

        public void Resume()
        {
            audioSource.StreamPaused = false;
        }

        public void Seek()
        {
            if (IsInstanceValid(playback))
            {
                bool isPlaying = audioSource.Playing;

                playback.Stop(); // Ensure playback is stopped before clearing the buffer

                playback.ClearBuffer();

                if (isPlaying)
                {
                    audioSource.Play();

                    if (audioSource.GetStreamPlayback() is AudioStreamGeneratorPlayback pb)
                        playback = pb;
                }
            }
        }

        public void PlayPackets(List<AVFrame> frames)
        {
            if (frames.Count == 0)
                return;

            foreach (var frame in frames)
                QueuePacket(frame);
        }

        private unsafe void QueuePacket(AVFrame frame)
        {
            pcm.Clear();

            pts = frame.pts;

            // Temporary storage for channel-specific PCM data
            List<float>[] channelData = new List<float>[channels];

            for (int i = 0; i < channels; i++)
                channelData[i] = [];

            for (uint ch = 0; ch < channels; ch++)
            {
                int size = ffmpeg.av_samples_get_buffer_size(null, 1, frame.nb_samples, sampleFormat, 1);

                if (size < 0)
                {
                    GD.PrintErr("audio buffer size is less than zero");
                    continue;
                }

                byte[] backBuffer2 = new byte[size];

                float[] backBuffer3 = new float[size / sizeof(float)];

                Marshal.Copy((IntPtr)frame.data[ch], backBuffer2, 0, size);

                Buffer.BlockCopy(backBuffer2, 0, backBuffer3, 0, backBuffer2.Length);

                channelData[ch].AddRange(backBuffer3);
            }

            if (playback.CanPushBuffer(1))
            {
                Vector2[] pcm2 = new Vector2[channelData[0].Count];

                for (int i = 0; i < pcm2.Length; i++)
                {
                    float left = channels > 0 ? channelData[0][i] : 0; // Left channel

                    float right = channels > 1 ? channelData[1][i] : left; // Right channel (if available)

                    pcm2[i] = new Vector2(left, right);
                }

                playback.PushBuffer(pcm2);
            }
        }
    }
}