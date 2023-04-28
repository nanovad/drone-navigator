// MainWindow
// The main window of the Review module, which contains a CDI control that is driven by previously saved mission data
// and video. The video contains familiar playback controls, and the CDI's drone data display synchronizes to that.
// See ReplaySynchronizer for more details.

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
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media.Core;
using Windows.Media.Playback;
using FlightDataModel;

namespace Review
{
    /// <summary>
    /// The main window of the Review module, which contains a CDI control that is driven by previously saved mission
    /// data and video, and enables media controls on the CDI's MediaPlayerElement for user interaction.
    /// See <see cref="ReplaySynchronizer"/>.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private ReplaySynchronizer? _synchronizer;
        public MissionModel? Mission;

        public MainWindow()
        {
            this.InitializeComponent();

            Title = "Drone Navigator - Review";
        }

        public void Initialize(MissionModel mission)
        {
            Mission = mission;
            _synchronizer = new ReplaySynchronizer(mission, Cdi.DroneVideoElement);
            Cdi.StatusProvider = _synchronizer;
        }
    }
}
