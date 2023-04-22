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
    /// An empty page that can be used on its own or navigated to within a Frame.
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

        private void RefreshList()
        {
            fdc.Drones.ToListAsync().ContinueWith(
                (items) => _dq.TryEnqueue(
                    () => DronesListView.ItemsSource = items.Result));
        }

        private async void NewDroneButton_Click(object sender, RoutedEventArgs e)
        {
            EditDroneDialog edd = new();
            edd.XamlRoot = this.Content.XamlRoot;
            if(await edd.ShowAsync() == ContentDialogResult.Primary)
                RefreshList();
        }

        private async void EditDroneButton_Click(object sender, RoutedEventArgs e)
        {
            DroneModel whichDrone = (e.OriginalSource as Button).DataContext as DroneModel;

            EditDroneDialog edd = new();
            edd.XamlRoot = this.Content.XamlRoot;
            edd.Prefill = whichDrone;
            if(await edd.ShowAsync() == ContentDialogResult.Primary)
                RefreshList();
        }

        private async void DeleteDroneButton_Click(object sender, RoutedEventArgs e)
        {
            DroneModel whichDrone = (e.OriginalSource as Button).DataContext as DroneModel;

            ContentDialog cd = new();
            cd.XamlRoot = this.Content.XamlRoot;
            cd.Title = "Delete drone?";
            cd.Content = $"Are you sure you want to delete the drone \"{whichDrone.Name}\"?";
            cd.PrimaryButtonText = "Delete";
            cd.SecondaryButtonText = "Cancel";

            ContentDialogResult res = await cd.ShowAsync();

            if(res == ContentDialogResult.Primary) {
                // TODO: We cannot simply remove the drone, as this will violate referential integrity.
                // We have to add a parameter to it to mark it as deleted.
                fdc.Remove(whichDrone);
                fdc.SaveChanges();
                RefreshList();
            }
        }

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
