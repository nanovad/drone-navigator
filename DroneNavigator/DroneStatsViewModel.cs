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
    /// <summary>
    /// A class following the ViewModel pattern that calculates statistics for a given drone from the database for use
    /// in UI elements that display the results of those calculations. See <see cref="DroneStats"/>.
    /// </summary>
    internal class DroneStatsViewModel
    {
        /// <summary>
        /// The maximum altitude ever flown by the given drone.
        /// </summary>
        public float MaxAltitudeFlown { get; private set; }
        /// <summary>
        /// The average duration of each mission flown by the given drone.
        /// </summary>
        public TimeSpan AverageTimeFlown { get; private set; }
        /// <summary>
        /// The cumulative time this drone has spent flying missions.
        /// </summary>
        public TimeSpan CumulativeTimeFlown { get; private set; }
        /// <summary>
        /// The number of missions this drone has flown.
        /// </summary>
        public int TotalMissionsFlown { get; private set; }
        /// <summary>
        /// The furthest distance from the controller this drone has ever been.
        /// </summary>
        public float FurthestDistanceFromController { get; private set; }

        private DroneStatsViewModel()
        {

        }

        /// <summary>
        /// Populates the fields contained in this class with the statistics for the drone given by
        /// <paramref name="droneId"/>.
        /// </summary>
        /// <param name="c">An active FlightDataContext object to use for database accesses.</param>
        /// <param name="droneId">The database ID of the drone to calculate these stats for.</param>
        /// <returns></returns>
        /// <exception cref="Exception">Thrown if the droneId parameter is an invalid database record ID.</exception>
        public static async Task<DroneStatsViewModel> Calculate(FlightDataContext c, int droneId)
        {
            // Attempt to locate the drone in the database by its ID.
            DroneModel? drone = c.Drones.Find(droneId);
            if (drone == null)
                throw new Exception($"Could not locate drone ID {droneId} in the database");

            DroneStatsViewModel ds = new();

            // Get a list of the missions this drone has flown.
            List<MissionModel> dronesMissions = await c.Missions.Where(
                m => m.Drone == drone.Id)
                .ToListAsync();

            // Calculate the average time flown during all these missions.
            ds.AverageTimeFlown = TimeSpan.FromSeconds(
                dronesMissions.Average(m => m.Duration.TotalSeconds));

            // Calculate highest barometric altitude.
            // For every mission...
            foreach(MissionModel m in dronesMissions) {
                // Select all flight states for this mission
                var relFlightStates = await c.FlightStates.Where(f => f.Mission == m.Id).ToListAsync();
                // If there are no flight states for this mission (a mission that never connected to the drone), skip.
                if(relFlightStates.Any()) {
                    // Calculate the maximum altitude attained for this mission.
                    var maxAlt = relFlightStates.Max(t => t.BarometricAltitude);
                    // The maximum altitude is the higher value from of any of the other missions, or this mission.
                    ds.MaxAltitudeFlown = Math.Max(ds.MaxAltitudeFlown, maxAlt);
                }
            }

            // Calculate the cumulative time flown by this drone during all its missions.
            TimeSpan cumulative = TimeSpan.Zero;
            dronesMissions.ForEach(m => cumulative = cumulative.Add(m.Duration));
            ds.CumulativeTimeFlown = cumulative;

            // Set the number of missions this drone has flown.
            ds.TotalMissionsFlown = dronesMissions.Count;

            // Calculate the furthest distance the drone flew during any mission.
            // For every mission...
            foreach (MissionModel m in dronesMissions)
            {
                // Select all flight states for this mission
                var relFlightStates = await c.FlightStates.Where(f => f.Mission == m.Id).ToListAsync();
                // If there are no flight states for this mission (a mission that never connected to the drone), skip.
                if(relFlightStates.Any()) {
                    // Calculate the maximum distance the drone flew from the controller during this mission.
                    var maxDist = relFlightStates.Max(t => t.TotalDistance);
                    // The maximum distance is the higher value from of any of the other missions, or this mission.
                    ds.FurthestDistanceFromController = Math.Max(ds.FurthestDistanceFromController, maxDist);
                }
            }

            return ds;
        }
    }
}
