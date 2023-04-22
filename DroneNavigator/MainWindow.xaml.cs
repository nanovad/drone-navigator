// MainWindow
// The UI entry point for the Drone Navigator project. Provides drone and mission management controls, and ways to
// start a new mission, or review a previously flown one.

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
using FlightDataModel;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace DroneNavigator
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            MainNavigationView.SelectedItem = MainNavigationView.MenuItems.ElementAt(0);
        }

        private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if(args.IsSettingsSelected)
            {
                // We do not have a settings page - short circuit
                return;
            }
            NavigationViewItem selected = (NavigationViewItem)args.SelectedItem;
            if(args.SelectedItem != null)
            {
                string tag = selected.Tag as string;
                string pageName = "DroneNavigator." + tag;
                Type pageType = Type.GetType(pageName);
                MainNavigationFrame.Navigate(pageType);
            }
        }
    }
}
