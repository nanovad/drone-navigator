// EditDroneDialog
// A dialog that presents a simple interface for modifying properties of a given drone in the database.
// This dialog is also used when creating a drone.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

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

namespace DroneNavigator
{
    /// <summary>
    /// A dialog that presents a simple interface for modifying properties of a given drone in the database.
    /// This dialog is also used when creating a drone.
    /// </summary>
    public sealed partial class EditDroneDialog : ContentDialog
    {
        /// <summary>
        /// Set to a DroneModel to prefill the fields of this dialog with values from a given drone. Note that this
        /// means this dialog box will act as an "edit" dialog, where the values that are changed will be committed
        /// back to the original drone object when the Save button is pressed.
        /// </summary>
        public DroneModel? Prefill { get; set; }

        public EditDroneDialog() : base()
        {
            this.InitializeComponent();
        }

        /// <summary>
        /// Shows the dialog.
        /// </summary>
        /// <returns>An asynchronous object containing a ContentDialogResult indicating what action the user took.</returns>
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

        /// <summary>
        /// Event handler for the dialog's "Save" button click.
        /// </summary>
        private void NewDroneDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Build a list of validation errors.
            List<string> errors = new();
            if(string.IsNullOrWhiteSpace(DroneNameTextBox.Text))
                errors.Add("drone name cannot be empty or only whitespace");
            if(string.IsNullOrWhiteSpace(DroneMakeTextBox.Text))
                errors.Add("drone make cannot be empty or only whitespace");
            if(string.IsNullOrWhiteSpace(DroneModelTextBox.Text))
                errors.Add("drone model cannot be empty or only whitespace");

            // If there are no errors, we can save the drone.
            if(errors.Count == 0)
            {
                // If the Prefill field was set, we can set our drone instance to that reference.
                // Otherwise, we'll need to create a new drone object.
                DroneModel? drone = Prefill switch {
                    null => new(),
                    _ => Prefill
                };
                drone.Name = DroneNameTextBox.Text;
                drone.Make = DroneMakeTextBox.Text;
                drone.Model = DroneModelTextBox.Text;

                // Open a new connection to the database.
                FlightDataContext fdc = new();
                // "Update" the record. If the record does not exist (i.e., it was freshly created, which occurs when
                // Prefill was null, a new record is created here.
                // Otherwise, the values changed during the edit operation are saved to the existing drone record.
                fdc.Update(drone);
                fdc.SaveChanges();
                return;
            }

            // Otherwise we have an error
            // Capitalize the first letter for aesthetics
            errors[0] = char.ToUpper(errors[0][0]) + errors[0][1..];
            ValidationMessageTextBox.Text = string.Join(", ", errors);
            ValidationMessageTextBox.Visibility = Visibility.Visible;
            // Prevent the dialog from closing.
            args.Cancel = true;
        }
    }
}
