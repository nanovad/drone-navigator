// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using FlightDataModel;
using Microsoft.EntityFrameworkCore;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DroneNavigator
{
    public sealed partial class MissionListPage : Page
    {

        private readonly FlightDataContext fdc;

        public MissionListPage()
        {
            this.InitializeComponent();
            Microsoft.UI.Dispatching.
                DispatcherQueue.GetForCurrentThread().TryEnqueue(Loaded);
            fdc = new FlightDataContext();
            fdc.Database.Migrate();
        }

        public async void Loaded()
        {
            List<MissionModel> sourceList = await fdc.Missions.ToListAsync();
            MissionListView.ItemsSource = sourceList.OrderByDescending(mission => mission.StartDateTimeOffset);
        }

        static internal string FormatMissionDuration(TimeSpan missionDuration)
        {
            return missionDuration.ToString("hh\\:mm\\:ss\\.fff");
        }

        private void ReplayMissionButton_OnClick(object sender, RoutedEventArgs e)
        {
            FrameworkElement el = (FrameworkElement)sender;
            MissionModel selectedMission = (MissionModel)el.DataContext;
            Review.MainWindow reviewWindow = new();
            reviewWindow.Initialize(selectedMission);
            reviewWindow.Activate();
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
            snmd.Drones = fdc.Drones.ToList();
            ContentDialog cd = new();
            cd.XamlRoot = this.Content.XamlRoot;
            cd.Title = "Start a new mission";
            cd.Content = snmd;
            cd.PrimaryButtonText = "Start";
            cd.CloseButtonText = "Cancel";
            cd.PrimaryButtonClick += delegate(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                if (snmd.DroneId == null)
                {
                    snmd.ErrorMessage = "Must select a drone";
                    args.Cancel = true;
                }
                else if(String.IsNullOrEmpty(snmd.MissionName)) {
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
                    Drone = snmd.DroneId ?? 0, // Just a safeguard - form validation should catch if DroneId is null
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
                (Application.Current as App)?.Window.Close();

                flightWindow.Activate();
                flightWindow.FlightFinished += delegate
                {
                    MainWindow newMain = new();
                    newMain.Activate();
                };
            }
        }
    }
}
