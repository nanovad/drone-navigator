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
