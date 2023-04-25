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
    /// The UI entry point for the Drone Navigator project.
    /// Holds a navigation frame and side pane enabling navigation to a MissionListPage (the default / landing page),
    /// or a DroneListPage.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        /// <summary>
        /// Initializes the page and navigates to its item #0, a DroneListPage.
        /// </summary>
        public MainWindow()
        {
            this.InitializeComponent();
            MainNavigationView.SelectedItem = MainNavigationView.MenuItems.ElementAt(0);
        }

        /// <summary>
        /// Event handler for when the user begins navigating to another page with the side pane's navigation buttons.
        /// </summary>
        private void MainNavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if(args.IsSettingsSelected)
            {
                // We do not have a settings page - short circuit
                return;
            }

            // Otherwise, get the item that the user selected.
            NavigationViewItem selected = (NavigationViewItem)args.SelectedItem;
            // Not sure when this could be null, but we want to check anyway.
            if(args.SelectedItem != null)
            {
                // The item's tag value is the class name.
                // This seems a little hacky, but it follows the example outlined in the NavigationView documentation.
                // Get a reference to the class for the desired page using the selected item's tag, then navigate to it.
                string tag = selected.Tag as string;
                string pageName = "DroneNavigator." + tag;
                Type pageType = Type.GetType(pageName);
                MainNavigationFrame.Navigate(pageType);
            }
        }
    }
}
