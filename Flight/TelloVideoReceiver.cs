using Microsoft.UI.Xaml;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.MediaProperties;

namespace Flight
{
    internal class TelloVideoReceiver
    {
        private readonly UdpClient _telloVideoClient;
        private IPEndPoint _telloEndPoint;

        private readonly VideoEncodingProperties _vep;
        public MediaStreamSource MediaStreamSource;

        private readonly Thread _receiveThread;
        private volatile bool _quitting = false;

        private readonly Stopwatch _watch = new();

        private readonly ConcurrentQueue<VideoSample> _samples = new();

        public TelloVideoReceiver(int telloVideoPort)
        {
            _telloVideoClient = new(telloVideoPort);
            _telloEndPoint = new IPEndPoint(IPAddress.Any, telloVideoPort);
            _telloVideoClient.Client.ReceiveTimeout = 1000;

            _vep = VideoEncodingProperties.CreateH264();
            _vep.Height = 720;
            _vep.Width = 960;

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


            if (_samples.TryDequeue(out VideoSample? videoSample))
            {
                args.Request.Sample = MediaStreamSample.CreateFromBuffer(videoSample.Raw.AsBuffer(), videoSample.Start);
            }
        }

        public void Quit()
        {
            this._quitting = true;
        }

        public void SynchronousReceiveVideo()
        {
            _watch.Start();
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

                    VideoSample vs = new(timeIndex, _watch.Elapsed - timeIndex, buf);
                    _samples.Enqueue(vs);
                }
                catch
                {
                }
            }
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
