using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using CDI;
using FlightDataModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Review
{
    internal class ReplaySynchronizer : IFlightStatusProvider
    {
        public event IFlightStatusProvider.OnFlightStatusChangedHandler? OnFlightStatusChanged;

        private readonly FlightDataContext _fdc = new();

        private readonly int _metOffset;
        private readonly List<Tuple<int, int>> _metList;

        private MediaPlayerElement _mpe;

        private readonly DispatcherTimer _flightStatusTimer = new();

        public ReplaySynchronizer(MissionModel mission, MediaPlayerElement mpe)
        {
            string missionVideoPath = MissionVideoManager.GetMissionVideoPath(mission);

            // BUG: We are not able to correctly determine the offset of the video vs the state logs here.
            TimeSpan videoDuration = FFMpegCore.FFProbe.Analyse(missionVideoPath).Duration;
            TimeSpan missionDuration = mission.EndDateTimeOffset - mission.StartDateTimeOffset;
            DateTimeOffset videoStartDateTime = mission.EndDateTimeOffset - videoDuration;
            _metOffset = (int)(mission.StartDateTimeOffset - videoStartDateTime).TotalMilliseconds;
            _metOffset = (int)(missionDuration - videoDuration).TotalMilliseconds;
            _metOffset = 0;

            _mpe = mpe;
            _mpe.AutoPlay = false;
            _mpe.Source = MediaSource.CreateFromUri(new Uri(missionVideoPath));
            _mpe.MediaPlayer.Play();
            _mpe.AreTransportControlsEnabled = true;

            _flightStatusTimer.Interval = TimeSpan.FromMilliseconds(10);
            _flightStatusTimer.Tick += FlightStatusTimerOnTick;
            _flightStatusTimer.Start();

            _metList = (
                from state in _fdc.FlightStates.Where(
                    (state) => state.Mission == mission.Id)
                select new Tuple<int, int>(state.Id, state.Met))
                .ToList();
        }

        private void FlightStatusTimerOnTick(object? sender, object e)
        {
            int curMillis = (int)_mpe.MediaPlayer.PlaybackSession.Position.TotalMilliseconds + _metOffset;
            int closestStateId =
                _metList.Aggregate((x, y) =>
                    Math.Abs(x.Item2 - curMillis) < Math.Abs(y.Item2 - curMillis) ? x : y)
                .Item1; // The mission ID is Item1 in the tuple
            FlightStateModel closestState = _fdc.FlightStates.Find(closestStateId) ?? new FlightStateModel();
            OnFlightStatusChanged?.Invoke(this, new FlightStatusChangedEventArgs(closestState));
        }
    }
}
