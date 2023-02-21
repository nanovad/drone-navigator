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
