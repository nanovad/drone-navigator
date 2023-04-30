// DroneListPage.xaml.cs
// A UI page showing a list of drones retrieved from the database, as well as buttons to view full statistics for a
// selected drone, create a new drone, or edit information about a drone.
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
using Microsoft.UI.Dispatching;
using Windows.Services.Maps;

namespace DroneNavigator
{
    /// <summary>
    /// A Page showing a list of drones retrieved from the database, as well as buttons to view full statistics for a
    /// selected drone, create a new drone, or edit information about a drone.
    /// This Page is intended for use by the main window's navigation frame.
    /// </summary>
    public sealed partial class DroneListPage : Page
    {
        FlightDataContext fdc = new();

        DispatcherQueue _dq = DispatcherQueue.GetForCurrentThread();

        public DroneListPage()
        {
            this.InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            RefreshList();
        }

        /// <summary>
        /// Refreshes the ListItem, which is the main component of this page, with a fresh list of drones received from
        /// the database.
        /// </summary>
        private async void RefreshList()
        {
            List<DroneModel> drones = await fdc.Drones.ToListAsync();
            _dq.TryEnqueue(() => DronesListView.ItemsSource = drones);

            if (drones.Count > 0)
            {
                NoDronesTextBlock.Visibility = Visibility.Collapsed;
                DronesListView.Visibility = Visibility.Visible;
            }
            else
            {
                NoDronesTextBlock.Visibility = Visibility.Visible;
                DronesListView.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Event handler for the "New Drone" button click.
        /// This spawns a dialog which allows the user to create a new drone with specified details or cancel the
        /// operation.
        /// </summary>
        private async void NewDroneButton_Click(object sender, RoutedEventArgs e)
        {
            // Spawn the EditDroneDialog, which does dual duty for creating drones.
            EditDroneDialog edd = new();
            edd.XamlRoot = this.Content.XamlRoot;

            // Refresh the drone list when the drone is created.
            if(await edd.ShowAsync() == ContentDialogResult.Primary)
                RefreshList();
            // Other results can be ignored, as they will not make database modifications.
        }

        /// <summary>
        /// Event handler for each drone's "Edit" button click.
        /// This spawns a dialog which allows the user to edit the selected drone.
        /// Must be called from the DronesListView's list item data context
        /// </summary>
        private async void EditDroneButton_Click(object sender, RoutedEventArgs e)
        {
            // Get the drone that this ListItem was built from.
            DroneModel whichDrone = (e.OriginalSource as Button).DataContext as DroneModel;

            // Spawn the EditDroneDialog prefilled with data from the drone.
            EditDroneDialog edd = new();
            edd.Title = "Edit drone";
            edd.XamlRoot = this.Content.XamlRoot;
            edd.Prefill = whichDrone;

            // Refresh the drone list when the drone is created.
            if(await edd.ShowAsync() == ContentDialogResult.Primary)
                RefreshList();
            // Other results can be ignored, as they will not make database modifications.
        }

        /// <summary>
        /// Event handler for each drone's analytics button click.
        /// Spawns a DroneStats dialog which shows information about the selected drone.
        /// </summary>
        private async void AnalyticsButton_OnClick(object sender, RoutedEventArgs e)
        {
            DroneModel drone = (e.OriginalSource as Button).DataContext as DroneModel;
            DroneStats ds = new(fdc, drone.Id);

            ContentDialog cd = new();
            cd.XamlRoot = this.Content.XamlRoot;
            cd.Title = "Analytics - " + drone.Name;
            cd.Content = ds;
            cd.CloseButtonText = "Close";

            await cd.ShowAsync();
        }
    }
}
