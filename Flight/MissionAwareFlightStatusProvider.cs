// MissionAwareFlightStatusProvider
// A class which injects the current mission into flight state instances received from the drone, as the API is unaware
// of the ongoing mission, and the UI and database expect the context of the current mission to be contained in each
// of those FlightStates.

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
using CDI;
using FlightDataModel;

namespace Flight
{
    /// <summary>
    /// Injects the current mission into flight state instances received from the drone, as the API is unaware of the
    /// ongoing mission, and the UI and database expect the context of the current mission to be contained in each of
    /// those FlightStates.
    /// </summary>
    internal class MissionAwareFlightStatusProvider : IFlightStatusProvider
    {
        public event IFlightStatusProvider.OnFlightStatusChangedHandler? OnFlightStatusChanged;

        private IFlightStatusProvider _subProvider;
        private MissionModel _mission;

        /// <summary>
        /// Initialize with a sub-provider that provides FlightStates that this class will inject the mission's context
        /// into.
        /// </summary>
        public MissionAwareFlightStatusProvider(IFlightStatusProvider subProvider, MissionModel mission)
        {
            _subProvider = subProvider;
            _mission = mission;
            _subProvider.OnFlightStatusChanged += SubProviderOnOnFlightStatusChanged;
        }

        /// <summary>
        /// Called when the sub-provider has a new flight state. Adds the mission's ID and the mission elapsed time to
        /// the passed flight state, then passes it to any consumers that are registered to this instance. See
        /// <see cref="IFlightStatusProvider"/> for a description of the interface used.
        /// </summary>
        private void SubProviderOnOnFlightStatusChanged(object sender, FlightStatusChangedEventArgs e)
        {
            e.FlightState.Mission = _mission.Id;
            e.FlightState.Met = (int)(DateTimeOffset.Now - _mission.StartDateTimeOffset).TotalMilliseconds;
            OnFlightStatusChanged?.Invoke(this, e);
        }
    }
}
