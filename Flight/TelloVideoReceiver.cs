// TelloVideoReceiver
// The class that receives video from the Tello in real time and displays it in the CDI's MediaPlayerElement, which
// ultimately provides a live view of the drone's camera to the pilot. This class also buffers video received from the
// drone in memory for later transcoding.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

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

        public MediaStreamSource MediaStreamSource;

        private readonly Thread _receiveThread;
        private volatile CancellationTokenSource _quitting = new();

        private readonly Stopwatch _watch = new();

        private readonly ConcurrentQueue<MediaStreamSample> _samples = new();
        private readonly ConcurrentQueue<MediaStreamSample> _encodeSamples = new();

        public static VideoEncodingProperties TelloVideoEncodingProperties
        {
            get
            {
                var vep = VideoEncodingProperties.CreateH264();
                vep.Height = 720;
                vep.Width = 960;
                vep.Bitrate = 4 * 1024 * 1024;
                return vep;
            }
        }

        public TelloVideoReceiver(int telloVideoPort)
        {
            _telloVideoClient = new(telloVideoPort);
            _telloEndPoint = new IPEndPoint(IPAddress.Any, telloVideoPort);
            _telloVideoClient.Client.ReceiveTimeout = 1000;

            MediaStreamSource = new MediaStreamSource(new VideoStreamDescriptor(TelloVideoEncodingProperties));
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
            _quitting.Cancel();
            if (_receiveThread.IsAlive)
                _receiveThread.Join();
            _telloVideoClient.Close();
        }

        public void SynchronousReceiveVideo()
        {
            _watch.Start();

            // Ensure video folder is created.
            Directory.CreateDirectory(MissionVideoManager.BaseVideoFolderPath);
            FileStream videoTempStream = new(MissionVideoManager.TempVideoPath, FileMode.Create, FileAccess.Write);
            BinaryWriter videoTempWriter = new(videoTempStream);

            while(!_quitting.IsCancellationRequested)
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
                    _encodeSamples.Enqueue(MediaStreamSample.CreateFromBuffer(buf.AsBuffer(), timeIndex));

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

        public ConcurrentQueue<MediaStreamSample> Stop()
        {
            _quitting.Cancel();
            return _encodeSamples;
        }
    }
}
