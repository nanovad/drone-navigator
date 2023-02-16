// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CDI
{
    public sealed partial class CDI : UserControl
    {
        internal FlightStatusController Fsc;

        private FlightStateModel CurFlightState { get { return Fsc.CurrentState; } }

        public CDI()
        {
            this.InitializeComponent();
            Fsc = new FlightStatusController(FlightStatusChangedEventHandler);
            // Mock fire the event handler to initialize the UI values
            FlightStatusChangedEventHandler(null, null);
        }

        private void FlightStatusChangedEventHandler(object Sender, EventArgs e)
        {
            RefreshMet();
        }

        void RefreshMet()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("MET: ");
            stringBuilder.Append(TimeSpan.FromMilliseconds(CurFlightState.Met).ToString("c"));
            this.MetTextBlock.Text = stringBuilder.ToString();
        }
    }
}

