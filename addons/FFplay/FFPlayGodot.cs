using System;
using System.IO;
using System.Threading;
using Godot;

namespace FFmpeg.Godot
{
    [GlobalClass]
    public partial class FFPlayGodot : Node
    {
        public FFTimings videoTimings;

        public FFTimings audioTimings;

        private Thread thread;

        public event Action OnEndReached;

        public event Action OnVideoEndReached;

        public event Action OnAudioEndReached;

        public event Action OnError;

        [Export]
        public double videoOffset = 0d;

        [Export]
        public double audioOffset = 0d;

        [Export]
        public FFTexturePlayer texturePlayer;

        [Export]
        public FFAudioPlayer audioPlayer;

        private double timeOffset = 0d;

        private double pauseTime = 0d;

        public bool IsPlaying { get; private set; } = false;

        public bool IsStream { get; private set; } = false;

        public bool IsPaused { get; private set; } = false;

        public static double TimeAsDouble => Time.GetTicksMsec() / 1000d;

        public double PlaybackTime => IsPaused ? pauseTime : TimeAsDouble - timeOffset;

        public double VideoTime => TimeAsDouble - timeOffset + videoOffset;

        public double AudioTime => TimeAsDouble - timeOffset + audioOffset;

        private volatile bool isSeeking = false;

        private double seekTime = 0;

        public void Play(string url)
        {
            Play(url, url);
        }

        public void Play(Stream streamV, Stream streamA)
        {
            IsPlaying = false;

            StopThread();

            OnDestroy();

            videoTimings = new FFTimings(streamV, AVMediaType.AVMEDIA_TYPE_VIDEO);

            audioTimings = new FFTimings(streamA, AVMediaType.AVMEDIA_TYPE_AUDIO);

            Init();
        }

        public void Play(string urlV, string urlA)
        {
            IsPlaying = false;

            StopThread();

            OnDestroy();

            videoTimings = new FFTimings(urlV, AVMediaType.AVMEDIA_TYPE_VIDEO);

            audioTimings = new FFTimings(urlA, AVMediaType.AVMEDIA_TYPE_AUDIO);

            Init();
        }

        private void Init()
        {
            if (audioTimings.IsInputValid)
                audioPlayer.Init(audioTimings.decoder.SampleRate, audioTimings.decoder.Channels, audioTimings.decoder.SampleFormat);

            if (videoTimings.IsInputValid)
            {
                timeOffset = TimeAsDouble - videoTimings.StartTime;
                IsStream = Mathf.Abs(videoTimings.StartTime) > 5d;
            }
            else
                timeOffset = TimeAsDouble;

            if (!videoTimings.IsInputValid && !audioTimings.IsInputValid)
            {
                IsPaused = true;

                StopThread();

                GD.PrintErr("AV not found");

                IsPlaying = false;

                OnError?.Invoke();
            }
            else
            {
                audioPlayer.Resume();

                RunThread();

                IsPlaying = true;
            }
        }

        public void Seek(double timestamp)
        {
            if (IsStream)
                return;

            isSeeking = true;

            seekTime = timestamp;

            if (!IsPlaying)
            {
                PerformSeek();
                isSeeking = false;
                UpdateContent();
            }
        }

        public double GetLength()
        {
            if (videoTimings != null && videoTimings.IsInputValid)
                return videoTimings.GetLength();

            if (audioTimings != null && audioTimings.IsInputValid)
                return audioTimings.GetLength();

            return 0d;
        }

        public void Pause()
        {
            if (IsPaused)
                return;

            pauseTime = PlaybackTime;

            audioPlayer.Pause();

            IsPaused = true;

            StopThread();

            IsPlaying = false;
        }

        public void Resume()
        {
            if (!IsPaused)
                return;

            StopThread();

            timeOffset = TimeAsDouble - pauseTime;

            audioPlayer.Resume();

            IsPaused = false;

            RunThread();

            IsPlaying = true;
        }

        private void Update()
        {
            if (!IsPaused)
            {
                if (videoTimings != null)
                {
                    if (videoTimings.IsEndOfFile())
                    {
                        Pause();

                        OnVideoEndReached?.Invoke();

                        OnEndReached?.Invoke();
                    }
                }

                if (audioTimings != null)
                {
                    if (audioTimings.IsEndOfFile())
                    {
                        Pause();

                        OnAudioEndReached?.Invoke();

                        OnEndReached?.Invoke();
                    }
                }
            }
        }

        private void ThreadUpdate()
        {
            GD.Print("ThreadUpdate Start");

            while (!IsPaused)
            {
                try
                {
                    if (isSeeking)
                    {
                        PerformSeek();

                        isSeeking = false;

                        continue;
                    }

                    UpdateContent();
                }
                catch (Exception e)
                {
                    GD.PushError(e);
                    break;
                }
            }

            GD.Print("ThreadUpdate Done");
        }

        private void PerformSeek()
        {
            GD.Print($"Seeking to {seekTime}");

            timeOffset = TimeAsDouble - seekTime;

            pauseTime = seekTime;

            videoTimings?.Seek(VideoTime);

            if (audioTimings != null)
            {
                audioTimings.Seek(AudioTime);

                audioTimings.GetFrames();

                audioPlayer.CallDeferred("Seek");
            }
        }

        private void UpdateContent()
        {
            if (videoTimings != null)
            {
                videoTimings.Update(VideoTime);
                texturePlayer.PlayPacket(videoTimings.GetFrame());
            }

            if (audioTimings != null)
            {
                audioTimings.Update(AudioTime);
                audioPlayer.PlayPackets(audioTimings.GetFrames());
            }
        }

        private void OnDestroy()
        {
            videoTimings?.Dispose();

            videoTimings = null;

            audioTimings?.Dispose();

            audioTimings = null;
        }

        private void RunThread()
        {
            if (thread.IsAlive)
                return;

            IsPaused = false;

            thread = new Thread(ThreadUpdate);

            thread.Start();
        }

        private void StopThread()
        {
            if (!thread.IsAlive)
                return;

            bool paused = IsPaused;

            IsPaused = true;

            thread.Join();

            IsPaused = paused;
        }

        public override void _Process(double delta)
        {
            Update();
        }

        public override void _EnterTree()
        {
            thread = new Thread(ThreadUpdate);
        }

        public override void _ExitTree()
        {
            IsPaused = true;

            StopThread();

            OnDestroy();
        }
    }
}