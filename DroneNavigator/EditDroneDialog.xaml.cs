// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using FlightDataModel;
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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace DroneNavigator
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class EditDroneDialog : ContentDialog
    {
        public DroneModel? Prefill { get; set; }

        public EditDroneDialog() : base()
        {
            this.InitializeComponent();
        }

        public new IAsyncOperation<ContentDialogResult> ShowAsync()
        {
            // TODO: Should we use DataContext?
            if(Prefill is not null) {
                DroneNameTextBox.Text = Prefill!.Name;
                DroneMakeTextBox.Text = Prefill!.Make;
                DroneModelTextBox.Text = Prefill!.Model;
            }
            return base.ShowAsync();
        }

        private void NewDroneDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            List<string> errors = new();
            if(string.IsNullOrWhiteSpace(DroneNameTextBox.Text))
                errors.Add("drone name cannot be empty or only whitespace");
            if(string.IsNullOrWhiteSpace(DroneMakeTextBox.Text))
                errors.Add("drone make cannot be empty or only whitespace");
            if(string.IsNullOrWhiteSpace(DroneModelTextBox.Text))
                errors.Add("drone model cannot be empty or only whitespace");

            if(errors.Count == 0)
            {
                DroneModel? drone = Prefill switch {
                    null => new(),
                    _ => Prefill
                };
                drone.Name = DroneNameTextBox.Text;
                drone.Make = DroneMakeTextBox.Text;
                drone.Model = DroneModelTextBox.Text;

                FlightDataContext fdc = new();
                fdc.Update(drone);
                fdc.SaveChanges();
                return;
            }

            // Otherwise we have an error
            // Capitalize the first letter for aesthetics
            errors[0] = char.ToUpper(errors[0][0]) + errors[0][1..];
            ValidationMessageTextBox.Text = string.Join(", ", errors);
            ValidationMessageTextBox.Visibility = Visibility.Visible;
            args.Cancel = true;
        }
    }
}
