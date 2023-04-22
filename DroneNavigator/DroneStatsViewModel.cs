// DroneStatsViewModel
// A class following the ViewModel pattern that calculates statistics for a given drone from the database for use in
// UI elements that display the results of those calculations.

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
using FlightDataModel;
using Microsoft.EntityFrameworkCore;

namespace DroneNavigator
{
    internal class DroneStatsViewModel
    {
        public float MaxAltitudeFlown { get; private set; }
        public TimeSpan AverageTimeFlown { get; private set; }
        public TimeSpan CumulativeTimeFlown { get; private set; }

        private DroneStatsViewModel()
        {

        }

        public static async Task<DroneStatsViewModel> Calculate(FlightDataContext c, int droneId)
        {
            DroneModel? drone = c.Drones.Find(droneId);
            if (drone == null)
                throw new Exception($"Could not locate drone ID {droneId} in the database");

            DroneStatsViewModel ds = new();

            List<MissionModel> dronesMissions = await c.Missions.Where(
                m => m.Drone == drone.Id)
                .ToListAsync();

            // Calculate average time flown
            ds.AverageTimeFlown = TimeSpan.FromSeconds(
                dronesMissions.Average(m => m.Duration.TotalSeconds));

            // Calculate highest barometric altitude
            foreach(MissionModel m in dronesMissions) {
                var relFlightStates = await c.FlightStates.Where(f => f.Mission == m.Id).ToListAsync();
                if(relFlightStates.Any()) {
                    var maxAlt = relFlightStates.Max(t => t.BarometricAltitude);
                    ds.MaxAltitudeFlown = Math.Max(ds.MaxAltitudeFlown, maxAlt);
                }
            }

            TimeSpan cumulative = TimeSpan.Zero;
            dronesMissions.ForEach(m => cumulative.Add(m.Duration));
            ds.CumulativeTimeFlown = cumulative;

            return ds;
        }
    }
}
