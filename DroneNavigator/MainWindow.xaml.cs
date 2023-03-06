// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
using FlightDataModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DroneNavigator
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private FlightDataContext fdc;

        public MainWindow()
        {
            this.InitializeComponent();
            Microsoft.UI.Dispatching.
                DispatcherQueue.GetForCurrentThread().TryEnqueue(Loaded);
            fdc = new FlightDataContext();
            fdc.Database.Migrate();
        }

        public void Loaded()
        {
            List<MissionModel> sourceList = fdc.Missions.ToList();
            MissionListView.ItemsSource = sourceList;
        }

        private void ReplayMissionButton_OnClick(object sender, RoutedEventArgs e)
        {
            FrameworkElement el = (FrameworkElement)sender;
            MissionModel selectedMission = (MissionModel)el.DataContext;
        }

        private void MissionListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Disable selection by immediately deselecting upon selecting.
            MissionListView.SelectedItem = null;
        }

        private async void StartNewMissionButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Spawn the "Start new mission" dialog.
            StartNewMissionDialog snmd = new();
            ContentDialog cd = new();
            cd.XamlRoot = this.Content.XamlRoot;
            cd.Content = snmd;
            cd.PrimaryButtonText = "Start";
            cd.CloseButtonText = "Cancel";
            cd.PrimaryButtonClick += delegate(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                // TODO: Validate snmd.DroneId?
                if(String.IsNullOrEmpty(snmd.MissionName)) {
                    snmd.ErrorMessage = "Must enter a mission name";
                    args.Cancel = true;
                }
                else if(String.IsNullOrEmpty(snmd.MissionDescription)) {
                    snmd.ErrorMessage = "Must enter a mission description";
                    args.Cancel = true;
                }
            };
            ContentDialogResult result = await cd.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // Commit the mission (as defined by the dialog's textboxes) to the DB
                MissionModel newMission = new()
                {
                    Drone = snmd.DroneId ?? 0, // BUG: Can't have a 0 DroneId, probably a recipe for breakage
                    Name = snmd.MissionName,
                    Description = snmd.MissionDescription,
                    StartDateTimeOffset = DateTimeOffset.Now
                };
                fdc.Missions.Add(newMission);
                fdc.SaveChanges();

                // EF Core will have generated an ID for newMission at this point, so we can use it here.
                // Begin initializing a new Flight window
                Flight.MainWindow flightWindow = new();
                flightWindow.Mission = newMission;
                // Close our window - we're about to swap to the Flight interface
                this.Close();

                flightWindow.Activate();
                flightWindow.Closed += delegate
                {
                    // When the Flight interface closes, spawn a new landing page
                    MainWindow newMain = new();
                    newMain.Activate();
                };
            }
        }
    }
}
