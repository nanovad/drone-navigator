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

        private DispatcherQueue _dq = DispatcherQueue.GetForCurrentThread();

        private FlightDataContext _fdc = new FlightDataContext();
        public MissionModel? Mission;

        public MainWindow()
        {
            this.InitializeComponent();
            api = new();
            Cdi.StatusProvider = api;
        }

        private void ConnectButton_OnClick(object sender, RoutedEventArgs e)
        {
            // If we are currently connected, this button turns into a disconnect button, so take that action instead.
            if(api.Connected) {
                // Disconnect (sets the api.Connected property to false, plus performs the actual disconnection).
                api.StopConnection();
                // Restore the button content.
                ConnectButton.Content = "Connect";
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

            Task.Run(() =>
            {
                try
                {
                    api.StartConnection();

                    _dq.TryEnqueue(() => {
                        // If StartConnection() does not throw an exception, we can assume the connection succeeded.
                        ConnectButton.Content = "Disconnect"; 
                        ConnectButton.IsEnabled = true;
                        ConnectionFailedInfoBar.IsOpen = false;
                    });
                }
                catch(TelloConnectionException _)
                {
                    // If StartConnection() did throw an exception, then we are not connected - reset the control to
                    // its original state and show the connection failure InfoBar.
                    _dq.TryEnqueue(() =>
                    {
                        ConnectButton.Content = "Connect";
                        ConnectButton.IsEnabled = true;
                        ConnectionFailedInfoBar.IsOpen = true;
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
    }
}
