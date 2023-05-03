// MainWindow
// The Flight module's main window, containing buttons for connecting to the drone, taking off, and landing, and the
// CDI, which displays information (data and video) received from the drone in real time.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

using FlightDataModel;
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
using System.Diagnostics;

namespace Flight
{
    /// <summary>
    /// The Flight module's main window, containing buttons for connecting to the drone, taking off, and landing, and
    /// the CDI, which displays information (data and video) received from the drone in real time.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        // Cdi is declared in XAML.

        private TelloApi? api;

        private readonly InputController _fic = new(50);

        private readonly DispatcherQueue _dq = DispatcherQueue.GetForCurrentThread();

        private readonly FlightDataContext _fdc = new();
        public MissionModel? Mission;

        private MissionAwareFlightStatusProvider? _mafsp;

        private bool _missionVideoToEncode = false;
        private bool _missionVideoEncoded = false;
        private bool _videoBegan = false;

        public event EventHandler? OnMissionVideoEncodingCompleted;
        // Fired when transcoding has completed and the Flight module is exiting.
        public event EventHandler? FlightFinished;

        private ConcurrentQueue<MediaStreamSample>? _previousSamples;

        public MainWindow()
        {
            this.InitializeComponent();

            Title = "Drone Navigator - Flight";

            Cdi.DroneVideoElement.AutoPlay = true;
            Cdi.DroneVideoElement.MediaPlayer.PlaybackSession.PositionChanged += delegate
            {
                if (!_videoBegan)
                {
                    Mission!.StartDateTimeOffset = DateTimeOffset.Now;
                }

                _videoBegan = true;
            };
        }

        /// <summary>
        /// Disconnects from the drone, shuts down background threads for drone communication and input polling, and
        /// releases their resources. This method also accumulates the video samples acquired by the drone and returns
        /// them.
        /// </summary>
        /// <returns>Video samples recorded by the drone.</returns>
        private ConcurrentQueue<MediaStreamSample> Cleanup()
        {
            // Disable the button while we attempt to shut down.
            ConnectButton.IsEnabled = false;

            _fdc.Update(Mission!);
            Mission!.EndDateTimeOffset = DateTimeOffset.Now;

            // Stop the video stream.
            var samples = api?.StopVideo();

            // Disconnect (sets the api.Connected property to false, plus performs the actual disconnection).
            api?.StopConnection();

            // Stop polling for input from the gamepad.
            _fic.EndInputPolling();

            // Clear the last frame from the video player.
            Cdi.DroneVideoElement.Source = null;

            // Save any outstanding DB changes.
            _fdc.SaveChanges();

            // Shut down all network ports and background threads.
            api?.Quit();

            // Restore the button content.
            ConnectButton.Content = "Connect";

            // Re-enable the button.
            ConnectButton.IsEnabled = true;

            // Free the API instance for later GC.
            api = null;

            return samples ?? new ConcurrentQueue<MediaStreamSample>();
        }

        /// <summary>
        /// Logs any incoming state packets to the database.
        /// </summary>
        private void MafspOnOnFlightStatusChanged(object sender, FlightStatusChangedEventArgs e)
        {
            _fdc.Add(e.FlightState);
        }

        /// <summary>
        /// Event handler for the Connect button's click event. If the drone is currently connected, this will
        /// disconnect from it. If the drone is not currently connected, it will attempt to connect.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ConnectButton_OnClick(object sender, RoutedEventArgs e)
        {
            // If we are currently connected, this button turns into a disconnect button, so take that action instead.
            if(api != null && api.Connected)
            {
                _previousSamples = Cleanup();
                return;
            }

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


            api = new();

            // Initialize the Tello API
            // Pass the API's video provider to the video player
            Cdi.DroneVideoElement.Source = new MediaPlaybackItem(
                MediaSource.CreateFromMediaStreamSource(api.VideoReceiver.MediaStreamSource));
            // Set the FlightInputController's target API (to send commands via) to the new TelloAPI instance
            _fic.TargetApi = api;

            // Wire the MAFSP to the freshly initialized API.
            _mafsp = new MissionAwareFlightStatusProvider(api, Mission!);
            // Hook the flight status changed event
            _mafsp.OnFlightStatusChanged += MafspOnOnFlightStatusChanged;
            Cdi.StatusProvider = _mafsp;

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
                            _fic.EndInputPolling();
                        });
                    }

                    api.StartVideo();
                    _missionVideoToEncode = true;

                    _dq.TryEnqueue(() =>
                    {
                        // _previousSamples should be null while we're connected.
                        _previousSamples = null;

                        // If StartConnection() does not throw an exception, we can assume the connection succeeded.
                        ConnectButton.Content = "Disconnect"; 
                        ConnectButton.IsEnabled = true;
                        ConnectionFailedInfoBar.IsOpen = false;
                        Cdi.DroneVideoElement.MediaPlayer.Play();
                        _fdc.Update(Mission!);
                        Mission!.StartDateTimeOffset = DateTimeOffset.Now;
                        _missionVideoToEncode = true;
                    });

                }
                catch(TelloConnectionException)
                {
                    // If StartConnection() did throw an exception, then we are not connected - reset the control to
                    // its original state, clean up the connections and threads, and show the connection failure
                    // InfoBar.
                    _dq.TryEnqueue(() =>
                    {
                        // No need to save samples here, as none have been recorded if the connection fails.
                        Cleanup();
                        ConnectionFailedInfoBar.IsOpen = true;
                        BadControllerInfoBar.IsOpen = false;
                    });
                }
            });
        }

        private void TakeoffButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(api?.Connected ?? false)
                api.Takeoff();
        }

        private void LandButton_OnClick(object sender, RoutedEventArgs e)
        {
            if(api?.Connected ?? false)
                api.Land();
        }

        private async void MainWindow_OnClosed(object sender, WindowEventArgs args)
        {
            // Short circuit if we've already encoded the video. This handler actually gets called twice - once when
            // the user initiates the window closing, and again when the OnEncodeCompleted callback is run.
            // This boolean ensures that we actually close the window the second time around.
            if (_missionVideoEncoded)
            {
                FlightFinished?.Invoke(this, EventArgs.Empty);
                return;
            }

            // Perform shutdown of all background tasks, threads, and connections.
            var queue = Cleanup();

            // Occurs when the user already clicked the Disconnect button (which retrieves the video samples) before
            // clicking the close button. If that's the case, the call to Cleanup just before this would return no
            // samples.
            if (_previousSamples != null)
                queue = _previousSamples;

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
                    OnMissionVideoEncodingCompleted?.Invoke(this, EventArgs.Empty);
                    // Notify the Main module that the flight has finished.
                    // The handler attached to this will close the input-blocking dialog.
                    FlightFinished?.Invoke(this, EventArgs.Empty);
                    // Close the Flight window.
                    this.Close();
                });
            };

            // Ensure our local app data folder (and video subfolder) is created.
            Directory.CreateDirectory(MissionVideoManager.BaseVideoFolderPath);

            if (_missionVideoToEncode && !queue.IsEmpty)
            {
                mep.StartEncoding(
                    queue,
                    TelloVideoReceiver.TelloVideoEncodingProperties,
                    MissionVideoManager.GetMissionVideoPath(Mission!));
                await mepd.ShowAsync();
            }
            else
            {
                FlightFinished?.Invoke(this, EventArgs.Empty);
                // Allow the window to close properly.
                args.Handled = false;
            }
        }
    }
}
