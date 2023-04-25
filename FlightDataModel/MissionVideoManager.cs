// MissionVideoManager
// A helper class that unifies path definitions for mission videos and contains helper methods to locate them on disk.

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

namespace FlightDataModel
{
    /// <summary>
    /// Unifies path definitions for mission videos and contains helper methods to locate them on disk.
    /// </summary>
    public static class MissionVideoManager
    {
        /// <summary>
        /// The base folder path for all videos; this is %LocalAppData%\DroneNavigator\video\
        /// </summary>
        public static string BaseVideoFolderPath => Path.Join(new string[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DroneNavigator",
            "video"
        });

        /// <summary>
        /// A constant path to the mission_video.raw temporary file.
        /// </summary>
        public static string TempVideoPath => Path.Join(BaseVideoFolderPath, "mission_video.raw");

        /// <summary>
        /// Gets a path to the corresponding video given an integer mission ID.
        /// Note that this function makes no guarantees that this file exists; however, if the mission video did exist,
        /// it would be located here.
        /// </summary>
        /// <param name="id">The ID of the mission to locate a video for.</param>
        /// <returns>The path where the video is expected to be.</returns>
        private static string VideoPathFromId(int id)
        {
            return Path.Join(BaseVideoFolderPath, $"{id:D}.mp4");
        }

        /// <summary>
        /// A wrapper around <see cref="VideoPathFromId(int)"/>. This wrapper accepts a MissionModel instead of an
        /// integer ID, and passes that MissionModel.Id to VideoPathFromId.
        /// </summary>
        /// <param name="mission">The mission to locate a video for.</param>
        /// <returns>The path where the video is expected to be.</returns>
        public static string GetMissionVideoPath(MissionModel mission)
        {
            return VideoPathFromId(mission.Id);
        }
    }
}
