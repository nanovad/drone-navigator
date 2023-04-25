// MissionListPage
// A page that displays a list of missions from the database, their summary information, and buttons for starting a new
// mission or reviewing a given mission.
// This Page is used by the main navigation frame.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

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

namespace DroneNavigator
{
    /// <summary>
    /// A Page that displays a list of missions from the database, their summary information, and buttons for starting
    /// a new mission or reviewing a given mission.
    /// This Page is intended for use by the main window's navigation frame.
    /// </summary>
    public sealed partial class MissionListPage : Page
    {

        private readonly FlightDataContext fdc;

        /// <summary>
        /// Initializes the Page, enqueues loading of the mission list, and performs a database migration if needed.
        /// </summary>
        public MissionListPage()
        {
            this.InitializeComponent();
            Microsoft.UI.Dispatching.
                DispatcherQueue.GetForCurrentThread().TryEnqueue(Loaded);
            fdc = new FlightDataContext();
            fdc.Database.Migrate();
        }

        /// <summary>
        /// Populates the list of missions with missions from the database.
        /// Invoked on the UI thread once the Page is loaded.
        /// </summary>
        public async void Loaded()
        {
            List<MissionModel> sourceList = await fdc.Missions.ToListAsync();
            MissionListView.ItemsSource = sourceList.OrderByDescending(mission => mission.StartDateTimeOffset);
        }

        /// <summary>
        /// Converts the given <paramref name="missionDuration"/> to a string with hours, minutes, seconds, and
        /// milliseconds in hh:mm:ss.fff format.
        /// </summary>
        /// <param name="missionDuration">The duration to format.</param>
        /// <returns>A formatted string in the hh:mm:ss.fff format.</returns>
        static internal string FormatMissionDuration(TimeSpan missionDuration)
        {
            return missionDuration.ToString("hh\\:mm\\:ss\\.fff");
        }

        /// <summary>
        /// Event handler for the "Replay" button click.
        /// Launches the Review module and passes it the currently selected mission.
        /// </summary>
        private void ReplayMissionButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Get the MissionModel that this ListItem is representing.
            FrameworkElement el = (FrameworkElement)sender;
            MissionModel selectedMission = (MissionModel)el.DataContext;

            // Spawn the Review main window with this ListItem's mission.
            Review.MainWindow reviewWindow = new();
            reviewWindow.Initialize(selectedMission);
            reviewWindow.Activate();
        }

        /// <summary>
        /// Event handler for when items in the list are selected. Selections are disallowed so this handler
        /// immediately deselects the item.
        /// </summary>
        private void MissionListView_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Disable selection by immediately deselecting upon selecting.
            MissionListView.SelectedItem = null;
        }

        /// <summary>
        /// Event handler for when the "Start New Mission" button is clicked.
        /// Spawns the Start New Mission dialog.
        /// </summary>
        private async void StartNewMissionButton_OnClick(object sender, RoutedEventArgs e)
        {
            // Initialize the "Start new mission" dialog.
            StartNewMissionDialog snmd = new();
            // Populate the drone list drop down from the database.
            snmd.Drones = fdc.Drones.ToList();

            // Create a new ContentDialog for this.
            // TODO: Move as much of this as possible to the StartNewMissionDialog class.
            ContentDialog cd = new();
            cd.XamlRoot = this.Content.XamlRoot;
            cd.Title = "Start a new mission";
            cd.Content = snmd;
            cd.PrimaryButtonText = "Start";
            cd.CloseButtonText = "Cancel";
            cd.PrimaryButtonClick += delegate(ContentDialog sender, ContentDialogButtonClickEventArgs args)
            {
                // Form validation.
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
            // Show the dialog.
            ContentDialogResult result = await cd.ShowAsync();

            // If the "Start" button was clicked.
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
                // Create a new entry in the database for this mission.
                fdc.Missions.Add(newMission);
                fdc.SaveChanges();

                // EF Core will have generated an ID for newMission at this point, so we can use it here.
                // Begin initializing a new Flight window
                Flight.MainWindow flightWindow = new();
                flightWindow.Mission = newMission;
                // Close our window - we're about to swap to the Flight interface
                (Application.Current as App)?.Window.Close();

                // Show the Flight window.
                flightWindow.Activate();
                flightWindow.FlightFinished += delegate
                {
                    // When the Flight completes (after transcocding), respawn the main window.
                    MainWindow newMain = new();
                    newMain.Activate();
                    // Necessary for subsequent Flight module starts; we want to reparent the app to the new
                    // MainWindow instance.
                    (Application.Current as App)!.Window = newMain;
                };
            }
        }
    }
}
