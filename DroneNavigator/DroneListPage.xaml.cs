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
using Microsoft.UI.Dispatching;
using Windows.Services.Maps;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

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
