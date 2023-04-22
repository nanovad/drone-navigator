// FlightStatusChangedEventArgs
// A data class used for passing FlightStates to callbacks.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlightDataModel;

namespace CDI
{
    public class FlightStatusChangedEventArgs : EventArgs
    {
        public FlightStateModel FlightState { get; set; }

        public FlightStatusChangedEventArgs() { }

        public FlightStatusChangedEventArgs(FlightStateModel state)
        {
            FlightState = state;
        }
    }
}
