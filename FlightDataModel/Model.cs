using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

namespace FlightDataModel
{
    internal class FlightDataContext : DbContext
    {
        public DbSet<DroneModel> Drones { get; set; }
        public DbSet<MissionModel> Missions { get; set; }
        public DbSet<CommandModel> Commands { get; set; }
        public DbSet<FlightState> FlightStates { get; set; }
    }

    internal class DroneModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Make { get; set; }
        public string Model { get; set; }
        public int TotalFlightTime { get; set; }
        public int MissionCount { get; set; }
    }

    internal class MissionModel
    {
        public int Id { get; set; }
        public int Drone { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTimeOffset StartDateTimeOffset { get; set; }
        public DateTimeOffset EndDateTimeOffset { get; set; }
        public float Distance { get; set; }
    }

    internal class CommandModel
    {
        public int Id { get; set; }
        public int Mission { get; set; }
        public DateTimeOffset SendDateTimeOffset { get; set; }
        public DateTimeOffset ResponseDateTimeOffset  {get; set; }
        public string Command { get; set; }
        public string Response { get; set; }
        public bool ResponseWasError { get; set; }
    }

    internal class FlightState
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

