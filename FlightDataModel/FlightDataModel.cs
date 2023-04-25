// FlightDataModel
// Data definitions for the entities contained in Drone Navigator's database, as well as hints to Entity Framework Core
// from which a database definition can be built. Entity Framework Core handles most of the lifting related to database
// accesses through the FlightDataContext class, allowing me to use a declarative approach to data modeling here.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace FlightDataModel
{
    /// <summary>
    /// Data definitions for the entities contained in Drone Navigator's database.
    /// </summary>
    public class FlightDataContext : DbContext
    {
        // These are each tables in the database; note that Commands was a planned feature but is not yet implemented.
        public DbSet<DroneModel> Drones { get; set; }
        public DbSet<MissionModel> Missions { get; set; }
        public DbSet<CommandModel> Commands { get; set; }
        public DbSet<FlightStateModel> FlightStates { get; set; }

        // A string containing the absolute path to the database.
        public string DbPath { get; }

        public FlightDataContext()
        {
            // We're using the LocalAppData path (usually C:\Users\<current user>\AppData\Local)
            string localAppDataPath = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroneNavigator");
            // Create the directory and all parents if they do not yet exist; no action is taken if they already exist.
            Directory.CreateDirectory(localAppDataPath);
            DbPath = Path.Join(localAppDataPath, "DroneNavigator.db");
        }

        // Tell EF Core:
        // 1. We are using SQLite.
        // 2. Where the SQLite database should be stored on disk.
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    /// <summary>
    /// The main data model for a Drone. Name, Make, and Model are user-entered fields, TotalFlightTime and
    /// MissionCount are calculated.
    /// </summary>
    public class DroneModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int TotalFlightTime { get; set; }
        public int MissionCount { get; set; }
    }

    /// <summary>
    /// The main data model for a Mission; some fields (Drone, Name, Description) are user-entered, some
    /// (StartDateTimeOffset, EndDateTimeOffset, Distance) are generated, some (Duration) are computed.
    /// </summary>
    public class MissionModel
    {
        public int Id { get; set; }
        public int Drone { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDateTimeOffset { get; set; }
        public DateTimeOffset EndDateTimeOffset { get; set; }
        public float Distance { get; set; }
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public TimeSpan Duration {
            get {
                if(EndDateTimeOffset < StartDateTimeOffset)
                    return TimeSpan.Zero;
                else
                    return EndDateTimeOffset - StartDateTimeOffset;
            }
        }
    }

    /// <summary>
    /// The main data model for a Command.
    /// NOTE: This exists in the database but is currently unused; a future improvement is planned.
    /// </summary>
    public class CommandModel
    {
        public int Id { get; set; }
        public int Mission { get; set; }
        public DateTimeOffset SendDateTimeOffset { get; set; }
        public DateTimeOffset ResponseDateTimeOffset  {get; set; }
        public string Command { get; set; }
        public string Response { get; set; }
        public bool ResponseWasError { get; set; }
    }

    /// <summary>
    /// The main data model for a FlightState, which are received from the drone during flight.
    /// </summary>
    public class FlightStateModel
    {
        public int Id { get; set; }
        public int Mission { get; set; }
        public int Met { get; set; }
        public int MotorTime { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
        public float Yaw { get; set; }
        public float Vgx { get; set; }
        public float Vgy { get; set; }
        public float Vgz { get; set; }
        public float Agx { get; set; }
        public float Agy { get; set; }
        public float Agz { get; set; }
        public float TotalDistance { get; set; }
        public float Altitude { get; set; }
        public float BarometricAltitude { get; set; }
        public float BatteryPercent { get; set; }
        public float BatteryTimeLeft { get; set; }
        public float Snr { get; set; }
    }
}

