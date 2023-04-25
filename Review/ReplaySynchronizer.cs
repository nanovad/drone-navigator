// ReplaySynchronizer
// Synchronizes flight states as displayed in the CDI during mission review.
// Timing and controls here are primarily driven by the MediaPlayerElement displaying the mission video, as its native
// controls will be most familiar to the user. Additionally, the strategy of keeping the flight data in sync with the
// video's position at any given moment ensures that seeking and playback rate changes will not cause desynchronization
// issues.

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
using Windows.Media.Core;
using Windows.Media.Playback;
using CDI;
using FlightDataModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Review
{
    /// <summary>
    /// Synchronizes flight states as displayed in the CDI during mission review.
    /// Timing and controls here are primarily driven by the MediaPlayerElement displaying the mission video, as its
    /// native controls will be most familiar to the user. Additionally, the strategy of keeping the flight data in
    /// sync with the video's position at any given moment ensures that seeking and playback rate changes will not
    /// cause desynchronization issues.
    /// </summary>
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

            // Transport control settings
            _mpe.AreTransportControlsEnabled = true;
            // Enable fast forward button & make it visible
            _mpe.TransportControls.IsFastForwardEnabled = true;
            _mpe.TransportControls.IsFastForwardButtonVisible = true;
            // Enable fast rewind button & make it visible
            _mpe.TransportControls.IsFastRewindEnabled = true;
            _mpe.TransportControls.IsFastRewindButtonVisible = true;
            // Disable volume button and hide it
            _mpe.TransportControls.IsVolumeEnabled = false;
            _mpe.TransportControls.IsVolumeButtonVisible = false;
            // Enable playback rate button and make it visible
            _mpe.TransportControls.IsPlaybackRateEnabled = true;
            _mpe.TransportControls.IsPlaybackRateButtonVisible = true;
            // Enable zoom controls and make them visible
            _mpe.TransportControls.IsZoomEnabled = true;
            _mpe.TransportControls.IsZoomButtonVisible = true;

            // Every 10ms, the timer will fire, and the CDI's data pane will be refreshed according to the mission
            // video's position. This interval is quick enough to ensure that the perception of realtime status updates
            // are maintained.
            _flightStatusTimer.Interval = TimeSpan.FromMilliseconds(10);
            _flightStatusTimer.Tick += FlightStatusTimerOnTick;
            _flightStatusTimer.Start();

            // Load all FlightStates from the database, converting each to a tuple containing the mission ID and the
            // MET field. This is done to reduce memory consumption, as we don't need to keep all FlightStates in RAM
            // at all time.
            _metList = (
                from state in _fdc.FlightStates.Where(
                    (state) => state.Mission == mission.Id)
                select new Tuple<int, int>(state.Id, state.Met))
                .ToList();
        }

        private void FlightStatusTimerOnTick(object? sender, object e)
        {
            // This is the current position of the video being played back, measured as milliseconds elapsed since the
            // beginning of the video.
            int curMillis = (int)_mpe.MediaPlayer.PlaybackSession.Position.TotalMilliseconds + _metOffset;

            // This bit of magic returns the ID of the flight state with an MET field closest to the current video
            // position. It is not guaranteed that a flight state record at the exact elapsed time of the video exists
            // in the database, and gaps in the state recordings are possible and supported (network issues may cause
            // a second or two of missing data from the drone), so the nearest flight state message is used.
            int closestStateId =
                _metList.Aggregate((x, y) =>
                    Math.Abs(x.Item2 - curMillis) < Math.Abs(y.Item2 - curMillis) ? x : y)
                .Item1; // The mission ID is Item1 in the tuple

            // Now, look the actual flight state record up according to its ID.
            // If that FlightState cannot be found, a blank FlightState is created to avoid crashing the UI with a null
            // reference.
            FlightStateModel closestState = _fdc.FlightStates.Find(closestStateId) ?? new FlightStateModel();
            // Invoke the event handler, which is hooked by the CDI to refresh the interface when it is called.
            OnFlightStatusChanged?.Invoke(this, new FlightStatusChangedEventArgs(closestState));
        }
    }
}
