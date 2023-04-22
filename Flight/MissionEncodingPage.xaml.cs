// MissionEncodingPage
// A Page for embedding in a ContentDialog that triggers after a mission is complete, kicking off the mission video
// transcoding process and displaying its progress.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.MediaProperties;
using Windows.Media.Transcoding;
using Windows.Storage.Streams;
using FFMpegCore;
using FFMpegCore.Enums;
using FlightDataModel;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;

namespace Flight
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MissionEncodingPage : Page
    {
        private readonly DispatcherQueue _dq = DispatcherQueue.GetForCurrentThread();

        public EventHandler? OnEncodeCompleted;

        public MissionEncodingPage()
        {
            this.InitializeComponent();
        }

        private void FFMpegEncodingProgressChanged(double progress)
        {
            if(double.IsInfinity(progress)) {
                return;
            }
            _dq.TryEnqueue(() => {
                EncodingProgressBar.Value = progress;
                PercentTextBlock.Text = $"{progress / 100:0:P1} complete"; //String.Format("{0:P1} complete", progress / 100.0);
            });
        }

        public async void StartEncoding(ConcurrentQueue<MediaStreamSample> queue, VideoEncodingProperties vep, string outPath)
        {
            MediaEncodingProfile mep = MediaEncodingProfile.CreateMp4(VideoEncodingQuality.HD720p);
            MediaStreamSource mss = new(new VideoStreamDescriptor(vep));

            // Get the largest timestamp in the queue, which will be the last sample in the video, which is equivalent
            // to the total duration of the video.
            mss.Duration = (from sample in queue.ToList() select sample.Timestamp).Max();

            // Wire the MediaStreamSource to the queue.
            mss.SampleRequested += delegate(MediaStreamSource sender, MediaStreamSourceSampleRequestedEventArgs args)
            {
                queue.TryDequeue(out MediaStreamSample? res);
                args.Request.Sample = res;
            };

            // Initialize the transcoder
            MediaTranscoder mt = new() { AlwaysReencode = true, HardwareAccelerationEnabled = true };
            PrepareTranscodeResult pmsst = await mt.PrepareMediaStreamSourceTranscodeAsync(mss,
                new FileStream(outPath, FileMode.Create, FileAccess.Write).AsRandomAccessStream(), mep);

            if(!pmsst.CanTranscode)
                throw new Exception("Unable to transcode mission video");

            var transcodeTask = pmsst.TranscodeAsync().AsTask(new Progress<double>((progress) => {
                _dq.TryEnqueue(() => {
                    EncodingProgressBar.Value = progress;
                    PercentTextBlock.Text = $"{progress / 100:P1} complete";
                });
            }));

            await transcodeTask;
            if(!transcodeTask.IsCompletedSuccessfully)
                throw new Exception("Failed to transcode the mission video");

            OnEncodeCompleted?.Invoke(this, EventArgs.Empty);
        }
    }
}
