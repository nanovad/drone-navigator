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
    internal class FlightStatusController
    {
        public event EventHandler OnChanged;

        private FlightStateModel PreviousState;
        private FlightStateModel currentState;
        public FlightStateModel CurrentState { get { return currentState; } }

        private MissionModel currentMission;
        public MissionModel CurrentMission { get { return currentMission; } } 
        private bool InFlight = false;

        private FlightDataContext Context;

        public FlightStatusController(EventHandler ChangedHandler)
        {
            OnChanged += ChangedHandler;
            Context = new FlightDataContext();
            currentState = new FlightStateModel();
        }

        public void InitializeMission(MissionModel Mission)
        {
            currentMission = Mission;
        }

        public void UpdateState(FlightStateModel NewState)
        {
            PreviousState = CurrentState;
            currentState = NewState;
            OnChanged(this, EventArgs.Empty);
        }
    }
}
