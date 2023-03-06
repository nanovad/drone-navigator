// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using FlightDataModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DroneNavigator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class StartNewMissionDialog : Page
    {
        public string MissionName => MissionNameTextBox.Text;
        public string MissionDescription => MissionDescTextBox.Text;
        public int? DroneId => (DroneComboBox.SelectionBoxItem as DroneModel)?.Id;
        public string ErrorMessage {
            get {
                return ErrorMessageTextBox.Text;
            }
            set {
                ErrorMessageTextBox.Text = value;
                if(String.IsNullOrEmpty(value)) {
                    // If there is no error message, collapse the control
                    // (i.e., make it invisible)
                    ErrorMessageTextBox.Visibility = Visibility.Collapsed;
                }
                else {
                    // If there is an error message, show the control.
                    ErrorMessageTextBox.Visibility = Visibility.Visible;
                }
            }
        }

        public StartNewMissionDialog()
        {
            this.InitializeComponent();
        }
    }
}
