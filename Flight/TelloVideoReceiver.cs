using Microsoft.UI.Xaml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage;
using FlightDataModel;

namespace Flight
{
    internal class TelloVideoReceiver
    {
        private readonly UdpClient _telloVideoClient;
        private IPEndPoint _telloEndPoint;

        private readonly VideoEncodingProperties _vep;
        public VideoEncodingProperties VideoEncodingProperties => _vep;
        public MediaStreamSource MediaStreamSource;

        private readonly Thread _receiveThread;
        private volatile bool _quitting = false;

        private readonly Stopwatch _watch = new();

        private readonly ConcurrentQueue<MediaStreamSample> _samples = new();

        public TelloVideoReceiver(int telloVideoPort)
        {
            _telloVideoClient = new(telloVideoPort);
            _telloEndPoint = new IPEndPoint(IPAddress.Any, telloVideoPort);
            _telloVideoClient.Client.ReceiveTimeout = 1000;

            _vep = VideoEncodingProperties.CreateH264();
            _vep.Height = 720;
            _vep.Width = 960;
            _vep.Bitrate = 5 * 1024 * 1024;

            MediaStreamSource = new MediaStreamSource(new VideoStreamDescriptor(_vep));
            MediaStreamSource.SampleRequested += MediaStreamSource_SampleRequested;
            _receiveThread = new Thread(SynchronousReceiveVideo);
        }

        private void MediaStreamSource_SampleRequested(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
        {
            if (_samples.IsEmpty)
            {
                SpinWait.SpinUntil(() => !_samples.IsEmpty);
            }

            if (_samples.TryDequeue(out MediaStreamSample? videoSample))
            {
                args.Request.Sample = videoSample;
            }
        }

        public void Quit()
        {
            this._quitting = true;
        }

        public void SynchronousReceiveVideo()
        {
            _watch.Start();

            // Ensure video folder is created.
            Directory.CreateDirectory(MissionVideoManager.BaseVideoFolderPath);
            FileStream videoTempStream = new(MissionVideoManager.TempVideoPath, FileMode.Create, FileAccess.Write);
            BinaryWriter videoTempWriter = new(videoTempStream);

            while(!_quitting)
            {
                TimeSpan timeIndex = this._watch.Elapsed;
                try
                {
                    byte[] buf = _telloVideoClient.Receive(ref _telloEndPoint);
                    if (buf.Length == 0)
                    {
                        continue;
                    }

                    //VideoSample vs = new(timeIndex, _watch.Elapsed - timeIndex, buf);
                    _samples.Enqueue(MediaStreamSample.CreateFromBuffer(buf.AsBuffer(), timeIndex));

                    videoTempStream.Write(buf);
                }
                catch
                {
                }
            }
            videoTempWriter.Close();
            videoTempStream.Close();
        }

        public void Start()
        {
            _receiveThread.Start();
        }

        public void Stop()
        {
            _quitting = true;
        }
    }
}
