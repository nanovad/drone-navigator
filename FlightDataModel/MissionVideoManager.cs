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
