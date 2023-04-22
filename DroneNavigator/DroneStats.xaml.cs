// DroneStats
// A page designed for use in ContentDialogs that displays statistics calculated for a specific drone.

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
using ABI.Microsoft.Web.WebView2.Core;
using FlightDataModel;
using Microsoft.UI.Dispatching;
namespace DroneNavigator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DroneStats : Page
    {
        internal DroneStatsViewModel? dsvm;

        /*public string AverageDistanceFlown => $"{dsvm?.AverageDistanceFlown:F1}cm";
        public string AverageTimeFlown => dsvm?.AverageTimeFlown.ToString("hh\\:mm\\:ss\\.fff") ?? "";*/
        public string AverageDistanceFlown { get; set; }
        public string AverageTimeFlown { get; set; }

        private readonly int _droneId;
        private readonly FlightDataContext _fdc;

        public DroneStats(FlightDataContext fdc, int droneId)
        {
            this.InitializeComponent();

            AverageDistanceFlown = "asdf";
            _fdc = fdc;
            _droneId = droneId;
        }


        private async void DroneStats_OnLoaded(object sender, RoutedEventArgs e)
        {
            dsvm = await DroneStatsViewModel.Calculate(_fdc, _droneId);

            MaxBarometricAltitude.Text = $"{dsvm.MaxAltitudeFlown:F1}cm";
            AverageTimeFlownTextBlock.Text = dsvm?.AverageTimeFlown.ToString("hh\\:mm\\:ss\\.fff") ?? "";
            CumulativeTimeFlownTextBlock.Text = dsvm?.AverageTimeFlown.ToString("dd\\:hh\\:mm\\:ss") ?? "";

            LoadingProgressRing.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
        }
    }
}
