using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FlightDataModel;
using Microsoft.EntityFrameworkCore;

namespace DroneNavigator
{
    internal class DroneStatsViewModel
    {
        public float AverageDistanceFlown { get; private set; }
        public TimeSpan AverageTimeFlown { get; private set; }

        private DroneStatsViewModel()
        {

        }

        public static async Task<DroneStatsViewModel> Calculate(FlightDataContext c, int droneId)
        {
            DroneModel? drone = c.Drones.Find(droneId);
            if (drone == null)
                throw new Exception($"Could not locate drone ID {droneId} in the database");

            DroneStatsViewModel ds = new();

            // Calculate average time flown
            ds.AverageTimeFlown = TimeSpan.FromSeconds(
                await c.Missions.Where(m => m.Drone == drone.Id).ToListAsync().ContinueWith(
                    t => t.Result.Average(m => m.Duration.TotalSeconds)));

            // Calculate average distance flown
            /*ds.AverageDistanceFlown = await c.Missions.Where(m => m.Drone == drone.Id)
                .AverageAsync(m => m.Distance);*/

            return ds;
        }
    }
}
