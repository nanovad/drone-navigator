// MissionVideoManager
// A helper class that unifies path definitions for mission videos and helper methods to locate them on disk.

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
    public static class MissionVideoManager
    {
        public static string BaseVideoFolderPath => Path.Join(new string[]
        {
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DroneNavigator",
            "video"
        });

        public static string TempVideoPath => Path.Join(BaseVideoFolderPath, "mission_video.raw");

        private static string VideoPathFromId(int id)
        {
            return Path.Join(BaseVideoFolderPath, $"{id:D}.mp4");
        }

        public static string GetMissionVideoPath(MissionModel mission)
        {
            return VideoPathFromId(mission.Id);
        }
    }
}
