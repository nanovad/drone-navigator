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
    /// A page designed for use in ContentDialogs that displays statistics calculated for a specific drone.
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

        /// <summary>
        /// Create a new instance of the DroneStats page. This prepares the page to calculate the statistics, but no DB
        /// accesses are performed until the Page is actually loaded by the interface and
        /// <see cref="DroneStats_OnLoaded"/> is called.
        /// </summary>
        /// <param name="fdc">An existing, open FlightDataContext object.</param>
        /// <param name="droneId">The ID of the drone to calculate statistics for.</param>
        public DroneStats(FlightDataContext fdc, int droneId)
        {
            this.InitializeComponent();

            // Initialize the stats strings with test values.
            AverageDistanceFlown = "asdf";
            // Initialize the database connection; this connection is the one already in use by DroneListPage, but it
            // should not cause concurrency problems, as no actions can be taken in that user interface until this
            // dialog is closed.
            _fdc = fdc;
            _droneId = droneId;
        }


        private async void DroneStats_OnLoaded(object sender, RoutedEventArgs e)
        {
            // Now that the page is ready, begin calculating statistics.
            // Note that the text boxes here are not yet visible because the MainGrid containing them is initialized
            // from XAML with their Visibility property set to Collapsed.
            // Instead, only a ProgresssRing is shown, which must be hidden when the calculation completes.
            
            // Actually perform calculation of the stats from the DB.
            dsvm = await DroneStatsViewModel.Calculate(_fdc, _droneId);

            // Format the strings nicely.
            MaxBarometricAltitude.Text = $"{dsvm.MaxAltitudeFlown:F1}cm";
            AverageTimeFlownTextBlock.Text = dsvm?.AverageTimeFlown.ToString("hh\\:mm\\:ss\\.fff") ?? "";
            CumulativeTimeFlownTextBlock.Text = dsvm?.CumulativeTimeFlown.ToString("dd\\:hh\\:mm\\:ss") ?? "";

            // Hide the ProgressRing that indicates to the user that work is ongoing.
            LoadingProgressRing.Visibility = Visibility.Collapsed;
            // And show the MainGrid containing the stats text boxes.
            MainGrid.Visibility = Visibility.Visible;
        }
    }
}
