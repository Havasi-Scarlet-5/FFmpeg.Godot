using System;
using System.Collections.Generic;
using System.IO;
using FFmpeg.Godot.Helpers;
using Godot;

namespace FFmpeg.Godot
{
    public class FFTimings : IDisposable
    {
        static FFTimings()
        {
            DynamicallyLinkedBindings.Initialize();
        }

        public FFmpegCtx context;

        public VideoStreamDecoder decoder;

        public bool IsInputValid;

        public double StartTime;

        private long pts;

        private AVRational timeBase;

        private double timeBaseSeconds;

        private AVPacket currentPacket;

        private AVFrame currentFrame;

        public FFTimings(string url, AVMediaType mediaType, AVHWDeviceType deviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
        {
            context = new FFmpegCtx(url);

            IsInputValid = context.HasStream(mediaType);

            Init(mediaType, deviceType);
        }

        public FFTimings(Stream stream, AVMediaType mediaType, AVHWDeviceType deviceType = AVHWDeviceType.AV_HWDEVICE_TYPE_NONE)
        {
            context = new FFmpegCtx(stream);

            IsInputValid = context.HasStream(mediaType);

            Init(mediaType, deviceType);
        }

        private void Init(AVMediaType type, AVHWDeviceType deviceType)
        {
            if (!IsInputValid)
                return;

            if (context.TryGetTimeBase(type, out timeBase))
            {
                timeBaseSeconds = ffmpeg.av_q2d(timeBase);

                decoder = new VideoStreamDecoder(context, type, deviceType);

                if (type == AVMediaType.AVMEDIA_TYPE_VIDEO && context.NextFrame(out AVPacket packet))
                {
                    StartTime = packet.dts * timeBaseSeconds;

                    AVFrame frame = DecodeFrame();

                    if (frame.format != -1)
                    {
                        currentPacket = packet;
                        currentFrame = frame;
                    }
                }

                GD.Print($"timeBase={timeBase.num}/{timeBase.den}");

                GD.Print($"timeBaseSeconds={timeBaseSeconds}");
            }
        }

        public void Update(double timestamp)
        {
            if (!IsInputValid)
                return;

            pts = (long)(Math.Max(double.Epsilon, timestamp) / timeBaseSeconds);
        }

        public void Seek(double timestamp)
        {
            if (!IsInputValid)
                return;

            context.Seek(decoder, timestamp);

            Update(timestamp);

            currentPacket = default;
        }

        public double GetLength()
        {
            if (!IsInputValid)
                return 0d;

            return context.GetLength(decoder);
        }

        public bool IsEndOfFile()
        {
            if (!IsInputValid)
                return false;

            return context.EndReached;
        }

        /// <summary>
        /// Returns the current frame for the active pts, decoding if needed
        /// </summary>
        public AVFrame GetCurrentFrame()
        {
            if (!IsInputValid)
                return new AVFrame()
                {
                    format = -1
                };

            return currentFrame;
        }

        public AVFrame GetFrame()
        {
            if (!IsInputValid)
                return new AVFrame()
                {
                    format = -1
                };

            while (pts >= currentPacket.dts || currentPacket.dts == ffmpeg.AV_NOPTS_VALUE)
            {
                if (context.NextFrame(out AVPacket packet))
                {
                    AVFrame frame = DecodeFrame();

                    if (frame.format != -1)
                    {
                        currentPacket = packet;
                        currentFrame = frame;
                    }
                }
                else
                    break;
            }

            return currentFrame;
        }

        public List<AVFrame> GetFrames()
        {
            if (!IsInputValid)
                return [];

            List<AVFrame> frames = [];

            while (pts >= currentPacket.dts || currentPacket.dts == ffmpeg.AV_NOPTS_VALUE)
            {
                if (context.NextFrame(out AVPacket packet))
                {
                    AVFrame frame = DecodeFrame();

                    if (frame.format != -1)
                    {
                        currentPacket = packet;

                        currentFrame = frame;

                        frames.Add(frame);
                    }
                }
                else
                    break;
            }

            return frames;
        }

        private AVFrame DecodeFrame()
        {
            decoder.Decode(out AVFrame frame);
            return frame;
        }

        private AVFrame DecodeMultiFrame()
        {
            int retCode;

            AVFrame frame;

            do
            {
                retCode = decoder.Decode(out frame);
            }
            while (retCode == 1);

            return frame;
        }

        public void Dispose()
        {
            decoder?.Dispose();

            context?.Dispose();

            GC.Collect();

            GC.WaitForPendingFinalizers();

            GC.SuppressFinalize(this);
        }
    }
}