// TelloVideoReceiver
// Receives video from the Tello in real time and provides it to a MediaStreamSource. Also buffers all video received
// from the drone, passing that buffer to whatever calls its Stop method. This is used for the transcoding process.

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
    /// <summary>
    /// Receives video from the Tello in real time and provides it to a MediaStreamSource. Also buffers all video
    /// received from the drone, passing that buffer to whatever calls its <see cref="Stop"/> method.
    /// </summary>
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

        /// <summary>
        /// Standard video encoding settings for the Tello, 4Mbits/s bitrate, 960 pixels wide by 720 pixels tall.
        /// </summary>
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

        /// <summary>
        /// Called by the MediaStreamSource when it is looking for a new video sample. Spins until a sample is placed
        /// into the buffer.
        /// </summary>
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

        /// <summary>
        /// Shuts down the video receiving thread. The instance of TelloVideoReceiver should not be reused once this
        /// method is called.
        /// </summary>
        public void Quit()
        {
            _quitting.Cancel();
            if (_receiveThread.IsAlive)
                _receiveThread.Join();
            _telloVideoClient.Close();
        }

        /// <summary>
        /// The method executed by the background thread. Continuously receives video samples over the network and
        /// places them into the <see cref="_samples"/> and <see cref="_encodeSamples"/> buffers.
        /// </summary>
        public void SynchronousReceiveVideo()
        {
            _watch.Start();

            // Ensure video folder is created.
            Directory.CreateDirectory(MissionVideoManager.BaseVideoFolderPath);
            FileStream videoTempStream = new(MissionVideoManager.TempVideoPath, FileMode.Create, FileAccess.Write);
            BinaryWriter videoTempWriter = new(videoTempStream);

            // Check to make sure that we haven't been asked to quit.
            while(!_quitting.IsCancellationRequested)
            {
                // Count up from when the thread was started; this is a continuously increasing timer to ensure that
                // samples do not arrive jumbled.
                TimeSpan timeIndex = this._watch.Elapsed;
                try
                {
                    byte[] buf = _telloVideoClient.Receive(ref _telloEndPoint);
                    // If we haven't received any video (probably a receive timeout), do nothing
                    if (buf.Length == 0)
                    {
                        continue;
                    }

                    //VideoSample vs = new(timeIndex, _watch.Elapsed - timeIndex, buf);
                    // Add the received sample to both the live buffer and the encoding buffer.
                    _samples.Enqueue(MediaStreamSample.CreateFromBuffer(buf.AsBuffer(), timeIndex));
                    _encodeSamples.Enqueue(MediaStreamSample.CreateFromBuffer(buf.AsBuffer(), timeIndex));

                    // Write the received sample to the temporary file as well.
                    videoTempStream.Write(buf);
                }
                catch
                {
                }
            }
            // Close the temporary file writer.
            videoTempWriter.Close();
            videoTempStream.Close();
        }

        public void Start()
        {
            _receiveThread.Start();
        }

        /// <summary>
        /// Notifies the background thread to stop receiving video.
        /// </summary>
        /// <returns>All video samples received during execution.</returns>
        public ConcurrentQueue<MediaStreamSample> Stop()
        {
            _quitting.Cancel();
            return _encodeSamples;
        }
    }
}
