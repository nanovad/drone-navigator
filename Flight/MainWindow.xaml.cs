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
using Windows.Foundation;
using Windows.Foundation.Collections;

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

        private TelloApi api;

        private FlightDataContext _fdc = new FlightDataContext();
        public MissionModel? Mission;

        public MainWindow()
        {
            this.InitializeComponent();
            api = new();
            api.StartConnection();
            Cdi.StatusProvider = api;
        }

        private void TakeoffButton_OnClick(object sender, RoutedEventArgs e)
        {
            api.Takeoff();
        }

        private void LandButton_OnClick(object sender, RoutedEventArgs e)
        {
            api.Land();
        }
    }
}
