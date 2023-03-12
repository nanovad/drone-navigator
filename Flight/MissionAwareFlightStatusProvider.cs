using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CDI;
using FlightDataModel;

namespace Flight
{
    internal class MissionAwareFlightStatusProvider : IFlightStatusProvider
    {
        public event IFlightStatusProvider.OnFlightStatusChangedHandler? OnFlightStatusChanged;

        private IFlightStatusProvider _subProvider;
        private MissionModel _mission;

        public MissionAwareFlightStatusProvider(IFlightStatusProvider subProvider, MissionModel mission)
        {
            _subProvider = subProvider;
            _mission = mission;
            _subProvider.OnFlightStatusChanged += SubProviderOnOnFlightStatusChanged;
        }

        private void SubProviderOnOnFlightStatusChanged(object sender, FlightStatusChangedEventArgs e)
        {
            e.FlightState.Mission = _mission.Id;
            e.FlightState.Met = (int)(DateTimeOffset.Now - _mission.StartDateTimeOffset).TotalMilliseconds;
            OnFlightStatusChanged?.Invoke(this, e);
        }
    }
}
