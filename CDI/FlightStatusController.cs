// FlightStatusController
// DEPRECATED: A component of the CDI obsoleted by the IFlightStatusProvider interface.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FlightDataModel;
using Microsoft.EntityFrameworkCore.Sqlite.Query.Internal;

namespace CDI
{
    internal class FlightStatusController : IFlightStatusProvider
    {
        private FlightStateModel PreviousState;
        private FlightStateModel currentState;
        public FlightStateModel CurrentState { get { return currentState; } }

        private MissionModel currentMission;
        public MissionModel CurrentMission { get { return currentMission; } } 
        private bool InFlight = false;

        private FlightDataContext Context;

        public event IFlightStatusProvider.OnFlightStatusChangedHandler OnFlightStatusChanged;

        public FlightStatusController(IFlightStatusProvider subProvider)
        {
            Context = new FlightDataContext();
            currentState = new FlightStateModel();

            // Hook ourselves into the sub-provider's callback, and propagate any updates from it to any handlers
            // registered to us.
            subProvider.OnFlightStatusChanged += SubProvider_OnFlightStatusChanged;
        }

        private void SubProvider_OnFlightStatusChanged(object sender, FlightStatusChangedEventArgs e)
        {
            UpdateState(e.FlightState);
        }

        public void InitializeMission(MissionModel Mission)
        {
            currentMission = Mission;
        }

        public void UpdateState(FlightStateModel NewState)
        {
            PreviousState = CurrentState;
            currentState = NewState;
            OnFlightStatusChanged?.Invoke(this, new FlightStatusChangedEventArgs(currentState));
        }
    }
}
