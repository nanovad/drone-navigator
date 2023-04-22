// CDI
// The Common Drone Interface, a shared control between the Flight and Review modules.
// This module provides the bulk of the Flight and Review interfaces. Its embedded MediaPlayerElement is exposed to
// other controls so that they can provide video either live from the drone or from a previous mission's video file.
// The rest of the controls pertain to quantitative flight data, and display information such as the mission elapsed
// time (or MET), speed, altitude, remaining battery, and other important data received from the drone as it flies.
// It is designed to be source-agnostic, which allows it to be reused by both Flight, which provides data in real time,
// and Review, which provides data synchronized to the mission video. (See Review.ReplaySynchronizer).

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
using System.Numerics;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Dispatching;

namespace CDI
{
    public sealed partial class CDI : UserControl
    {
        private DispatcherQueue _dq = DispatcherQueue.GetForCurrentThread();
        internal FlightStatusController Fsc;

        private volatile float initialBaroAlt = 0.0f;

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
            if(initialBaroAlt < 0.1f && initialBaroAlt > -0.1f) {
                initialBaroAlt = e.FlightState.BarometricAltitude;
            }
            _dq.TryEnqueue(() =>
            {
                RefreshMet(state);
                RefreshSnr(state);
                RefreshBattery(state);
                RefreshSpeed(state);
                RefreshDistance(state);
                RefreshAltitude(state);
            });
        }

        private void RefreshMet(FlightStateModel state)
        {
            this.MetTextBlock.Text = $"MET: {TimeSpan.FromMilliseconds(state.Met):hh\\:mm\\:ss\\.fff}";
        }

        private void RefreshSnr(FlightStateModel state)
        {
            state.Snr = state.Snr;
        }

        private void RefreshBattery(FlightStateModel state)
        {
            float batteryPct = state.BatteryPercent;
            this.BatteryIcon.Glyph = BatteryGlyph.FromPercent(batteryPct);
            this.BatteryTextBlock.Text = $"Batt: {(int)batteryPct:D}%";
        }

        private void RefreshSpeed(FlightStateModel state)
        {
            Vector3 v = new Vector3(state.Vgx, state.Vgy, state.Vgz);
            double dmsSpeedAbs = v.Length();
            double cmsSpeedAbs = dmsSpeedAbs * 10;
            double mphSpeedAbs = cmsSpeedAbs * 0.022369362912;
            this.SpeedTextBlock.Text = $"{mphSpeedAbs:f2}MPH";
        }

        private void RefreshDistance(FlightStateModel state)
        {
            float distanceCm = state.TotalDistance;
            float distanceFt = distanceCm * 0.0328084f; // Conversion factor cm->ft
            this.TotalDistanceTextBlock.Text = $"{distanceFt:f2}ft";
        }

        private void RefreshAltitude(FlightStateModel state)
        {
            float altFt = state.Altitude * 0.0328084f; // Conversion factor cm->ft
            this.VsAltitudeTextBlock.Text = $"VS: {altFt:f2}ft";

            // Calibrate the takeoff point to be tared with the initialBaroAlt, which aligns better with VS altitude.
            float baroAltFt = (state.BarometricAltitude - initialBaroAlt)  * 3.28084f;
            this.BaroAltitudeTextBlock.Text = $"Baro: {baroAltFt:f2}ft";
        }
    }
}

