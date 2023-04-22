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
    public class FlightDataContext : DbContext
    {
        public DbSet<DroneModel> Drones { get; set; }
        public DbSet<MissionModel> Missions { get; set; }
        public DbSet<CommandModel> Commands { get; set; }
        public DbSet<FlightStateModel> FlightStates { get; set; }

        public string DbPath { get; }

        public FlightDataContext()
        {
            string localAppDataPath = Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DroneNavigator");
            Directory.CreateDirectory(localAppDataPath);
            DbPath = Path.Join(localAppDataPath, "DroneNavigator.db");
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
            optionsBuilder.UseSqlite($"Data Source={DbPath}");
    }

    public class DroneModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int TotalFlightTime { get; set; }
        public int MissionCount { get; set; }
    }

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

