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
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Dispatching;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CDI
{
    public sealed partial class CDI : UserControl
    {
        private DispatcherQueue _dq = DispatcherQueue.GetForCurrentThread();
        internal FlightStatusController Fsc;

        private IFlightStatusProvider _statusProvider;
        public IFlightStatusProvider StatusProvider
        {
            get => _statusProvider;
            set
            {
                // Deregister our event handler hook from the current IFlightStatusProvider.
                if (_statusProvider is not null)
                    _statusProvider.OnFlightStatusChanged -= FlightStatusChangedEventHandler;
                _statusProvider = value;
                // Defensive programming here - attempt to deregister our event handler from the incoming
                // IFlightStatusProvider just in case we're already hooked to it. This could avoid duplicate handler
                // calls.
                // This has no effect if we are not already registered.
                _statusProvider.OnFlightStatusChanged -= FlightStatusChangedEventHandler;
                // Now register our handler to the event.
                _statusProvider.OnFlightStatusChanged += FlightStatusChangedEventHandler;
            }
        }

        private FlightStateModel CurFlightState => Fsc.CurrentState;

        public MediaPlayerElement DroneVideoElement => this.DroneVideoPlayer;

        public CDI()
        {
            this.InitializeComponent();
            // Mock fire the event handler to initialize the UI values.
            FlightStatusChangedEventHandler(null, new FlightStatusChangedEventArgs(new FlightStateModel()));
        }

        private void FlightStatusChangedEventHandler(object sender, FlightStatusChangedEventArgs e)
        {
            FlightStateModel state = e.FlightState;
            _dq.TryEnqueue(() =>
            {
                RefreshMet(state);
                RefreshSnr(state);
                RefreshSpeed(state);
                RefreshAltitude(state);
            });
        }

        private void RefreshMet(FlightStateModel state)
        {
            StringBuilder stringBuilder = new();
            stringBuilder.Append("MET: ");
            stringBuilder.Append(TimeSpan.FromMilliseconds(state.Met).ToString("c"));
            this.MetTextBlock.Text = stringBuilder.ToString();
        }

        private void RefreshSnr(FlightStateModel state)
        {
            state.Snr = state.Snr;
        }

        private void RefreshSpeed(FlightStateModel state)
        {
            Vector3 v = new Vector3(state.Vgx, state.Vgy, state.Vgz);
            this.SpeedTextBlock.Text = $"{v.Length()}MPH";
        }

        private void RefreshAltitude(FlightStateModel state)
        {
            this.AltitudeTextBlock.Text = $"Altitude: {state.Altitude}";
        }
    }
}

