using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using CDI;
using FlightDataModel;
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
            _mpe.MediaPlayer.PlaybackSession.PositionChanged += PlaybackSessionOnPositionChanged;
            _mpe.AreTransportControlsEnabled = true;

            _metList = (
                from state in _fdc.FlightStates.Where(
                    (state) => state.Mission == mission.Id)
                select new Tuple<int, int>(state.Id, state.Met))
                .ToList();
        }

        private void PlaybackSessionOnPositionChanged(MediaPlaybackSession sender, object args)
        {
            int curMillis = (int)sender.Position.TotalMilliseconds + _metOffset;
            int closestStateId =
                _metList.Aggregate((x, y) =>
                    Math.Abs(x.Item2 - curMillis) < Math.Abs(y.Item2 - curMillis) ? x : y)
                .Item1; // The mission ID is Item1 in the tuple
            FlightStateModel closestState = _fdc.FlightStates.Find(closestStateId) ?? new FlightStateModel();
            //FlightStateModel closestState = _fdc.FlightStates.Where((state) => state.Mission == _mission.Id).ToList().Aggregate((x,y) => Math.Abs(x.Met-curMillis) < Math.Abs(y.Met-curMillis) ? x : y);
            OnFlightStatusChanged?.Invoke(this, new FlightStatusChangedEventArgs(closestState));
        }
    }
}
