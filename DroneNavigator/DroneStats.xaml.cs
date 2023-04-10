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
using ABI.Microsoft.Web.WebView2.Core;
using FlightDataModel;
using Microsoft.UI.Dispatching;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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

            AverageDistanceFlownTextBlock.Text = $"{dsvm.AverageDistanceFlown:F1}cm";
            AverageTimeFlownTextBlock.Text = dsvm?.AverageTimeFlown.ToString("hh\\:mm\\:ss\\.fff") ?? "";

            LoadingProgressRing.Visibility = Visibility.Collapsed;
            MainGrid.Visibility = Visibility.Visible;
        }
    }
}
