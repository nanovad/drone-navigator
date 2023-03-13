// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FFMpegCore;
using FFMpegCore.Enums;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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

        public void StartEncoding(string tempFilePath, string outFilePath)
        {
            TimeSpan videoDuration = FFProbe.Analyse(tempFilePath).Duration;
            var args = FFMpegCore.FFMpegArguments.FromFileInput(tempFilePath)
                .OutputToFile(outFilePath, true, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithFramerate(30.00))
                    .NotifyOnProgress(FFMpegEncodingProgressChanged, videoDuration);
            args.ProcessAsynchronously().ContinueWith(t => {
                OnEncodeCompleted?.Invoke(this, EventArgs.Empty);
            });
        }
    }
}
