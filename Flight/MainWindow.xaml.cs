// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using FlightDataModel;
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
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Dispatching;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using CDI;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Flight
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // Cdi is declared in XAML.

        private readonly TelloApi api;

        private readonly InputController _fic = new(50);

        private readonly DispatcherQueue _dq = DispatcherQueue.GetForCurrentThread();

        private readonly FlightDataContext _fdc = new();
        public MissionModel? Mission;

        private MissionAwareFlightStatusProvider? _mafsp;

        private bool _missionVideoToEncode = false;
        private bool _missionVideoEncoded = false;
        private bool _videoBegan = false;

        public event EventHandler? OnMissionVideoEncodingCompleted;

        public MainWindow()
        {
            this.InitializeComponent();
            api = new();

            Cdi.DroneVideoElement.AutoPlay = true;
            Cdi.DroneVideoElement.MediaPlayer.RealTimePlayback = true;
            Cdi.DroneVideoElement.Source = new MediaPlaybackItem(
                MediaSource.CreateFromMediaStreamSource(api.VideoReceiver.MediaStreamSource));
            Cdi.DroneVideoElement.MediaPlayer.PlaybackSession.PositionChanged += delegate
            {
                if (!_videoBegan)
                {
                    Mission!.StartDateTimeOffset = DateTimeOffset.Now;
                }

                _videoBegan = true;
            };
            _fic.TargetApi = api;
        }

        private void MafspOnOnFlightStatusChanged(object sender, FlightStatusChangedEventArgs e)
        {
            _fdc.Add(e.FlightState);
        }

        private void ConnectButton_OnClick(object sender, RoutedEventArgs e)
        {
            // If we are currently connected, this button turns into a disconnect button, so take that action instead.
            if(api.Connected)
            {
                _fdc.Update(Mission!);
                Mission!.EndDateTimeOffset = DateTimeOffset.Now;

                // Stop the video stream.
                api.StopVideo();

                // Disconnect (sets the api.Connected property to false, plus performs the actual disconnection).
                api.StopConnection();

                // Stop polling for input from the gamepad.
                _fic.EndInputPolling();

                // Clear the last frame from the video player.
                Cdi.DroneVideoElement.Source = null;

                // Save any outstanding DB changes.
                _fdc.SaveChanges();

                // Restore the button content.
                ConnectButton.Content = "Connect";
                return;
            }

            _mafsp = new MissionAwareFlightStatusProvider(api, Mission!);
            _mafsp.OnFlightStatusChanged += MafspOnOnFlightStatusChanged;
            Cdi.StatusProvider = _mafsp;

            // Create a StackPanel that will hold a ProgressRing and a TextBlock that indicates we are connecting.
            StackPanel buttonStack = new() { Orientation = Orientation.Horizontal };
            TextBlock connectionText = new() {
                Text = "Connecting...",
                VerticalAlignment = VerticalAlignment.Center
            };
            // Ensure that the measurement properties are initialized before we try to read the ActualSize property.
            connectionText.Measure(new Size());
            buttonStack.Children.Add(new ProgressRing() {
                Margin = new Thickness(5.0),
                // Make the ProgressRing a circle with approximately the same diameter as the text is tall.
                MinHeight = connectionText.ActualHeight,
                MinWidth = connectionText.ActualHeight,
                Height = connectionText.ActualHeight,
                Width = connectionText.ActualHeight
            });
            buttonStack.Children.Add(connectionText);
            ConnectButton.Content = buttonStack;

            // Disable the button while we attempt a connection.
            ConnectButton.IsEnabled = false;

            Task.Run(() =>
            {
                try
                {
                    api.StartConnection();

                    // Start up gamepad polling
                    try
                    {
                        _fic.Initialize();
                        _dq.TryEnqueue(() =>
                        {
                            BadControllerInfoBar.IsOpen = false;
                        });
                        _fic.BeginInputPolling();
                    }
                    catch (ControllerNotFoundException cnfe)
                    {
                        _dq.TryEnqueue(() =>
                        {
                            BadControllerInfoBar.IsOpen = true;
                            BadControllerInfoBar.Message = cnfe.Message;
                        });
                    }

                    api.StartVideo();
                    _missionVideoToEncode = true;

                    _dq.TryEnqueue(() => {
                        // If StartConnection() does not throw an exception, we can assume the connection succeeded.
                        ConnectButton.Content = "Disconnect"; 
                        ConnectButton.IsEnabled = true;
                        ConnectionFailedInfoBar.IsOpen = false;
                        Cdi.DroneVideoElement.MediaPlayer.Play();
                        _fdc.Update(Mission!);
                        Mission!.StartDateTimeOffset = DateTimeOffset.Now;
                    });

                }
                catch(TelloConnectionException)
                {
                    // If StartConnection() did throw an exception, then we are not connected - reset the control to
                    // its original state and show the connection failure InfoBar.
                    _dq.TryEnqueue(() =>
                    {
                        ConnectButton.Content = "Connect";
                        ConnectButton.IsEnabled = true;
                        ConnectionFailedInfoBar.IsOpen = true;
                        BadControllerInfoBar.IsOpen = false;
                    });
                }
            });
        }

        private void TakeoffButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(api.Connected)
                api.Takeoff();
        }

        private void LandButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(api.Connected)
                api.Land();
        }

        private async void MainWindow_OnClosed(object sender, WindowEventArgs args)
        {
            // Short circuit if we've already encoded the video. This handler actually gets called twice - once when
            // the user initiates the window closing, and again when the OnEncodeCompleted callback is run.
            // This boolean ensures that we actually close the window the second time around.
            if(_missionVideoEncoded)
                return;

            if (api.Connected)
            {
                api.StopVideo();
                _fdc.Update(Mission!);
                //Mission!.EndDateTimeOffset = DateTimeOffset.Now;
            }
            api.StopConnection();

            // Track if any video was actually recorded - if not, there is nothing to encode.
            if (!_missionVideoToEncode)
                return;

            // Save any changes (probably FlightState) to the database.
            _fdc.SaveChanges();

            // At this point, we have a mission video that needs to be encoded.

            // Prevent the window from automatically closing.
            args.Handled = true;

            MissionEncodingPage mep = new();
            ContentDialog mepd = new ContentDialog();
            mepd.XamlRoot = this.Content.XamlRoot;
            mepd.Content = mep;
            mepd.Title = "Please wait while the mission video is saved...";
            mep.OnEncodeCompleted += (sender, args) => {
                _missionVideoEncoded = true;
                _dq.TryEnqueue(() => {
                    mepd.Hide();
                    OnMissionVideoEncodingCompleted?.Invoke(this, EventArgs.Empty);
                    this.Close();
                });
            };

            // Ensure our local app data folder (and video subfolder) is created.
            Directory.CreateDirectory(MissionVideoManager.BaseVideoFolderPath);

            if (!File.Exists(MissionVideoManager.TempVideoPath))
                throw new FileNotFoundException("Unable to find mission video file");

            mep.StartEncoding(MissionVideoManager.TempVideoPath, MissionVideoManager.GetMissionVideoPath(Mission!));
            await mepd.ShowAsync();
        }
    }
}
