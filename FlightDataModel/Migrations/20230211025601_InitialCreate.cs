using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FlightDataModel.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Commands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Mission = table.Column<int>(type: "INTEGER", nullable: false),
                    SendDateTimeOffset = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    ResponseDateTimeOffset = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Command = table.Column<string>(type: "TEXT", nullable: false),
                    Response = table.Column<string>(type: "TEXT", nullable: false),
                    ResponseWasError = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Commands", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Drones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Make = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    TotalFlightTime = table.Column<int>(type: "INTEGER", nullable: false),
                    MissionCount = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Drones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FlightStates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Mission = table.Column<int>(type: "INTEGER", nullable: false),
                    Met = table.Column<int>(type: "INTEGER", nullable: false),
                    MotorTime = table.Column<int>(type: "INTEGER", nullable: false),
                    Pitch = table.Column<float>(type: "REAL", nullable: false),
                    Roll = table.Column<float>(type: "REAL", nullable: false),
                    Yaw = table.Column<float>(type: "REAL", nullable: false),
                    Vgx = table.Column<float>(type: "REAL", nullable: false),
                    Vgy = table.Column<float>(type: "REAL", nullable: false),
                    Vgz = table.Column<float>(type: "REAL", nullable: false),
                    Agx = table.Column<float>(type: "REAL", nullable: false),
                    Agy = table.Column<float>(type: "REAL", nullable: false),
                    Agz = table.Column<float>(type: "REAL", nullable: false),
                    TotalDistance = table.Column<float>(type: "REAL", nullable: false),
                    Altitude = table.Column<float>(type: "REAL", nullable: false),
                    BarometricAltitude = table.Column<float>(type: "REAL", nullable: false),
                    BatteryPercent = table.Column<float>(type: "REAL", nullable: false),
                    BatteryTimeLeft = table.Column<float>(type: "REAL", nullable: false),
                    Snr = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FlightStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Missions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Drone = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    StartDateTimeOffset = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    EndDateTimeOffset = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Distance = table.Column<float>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Missions", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Commands");

            migrationBuilder.DropTable(
                name: "Drones");

            migrationBuilder.DropTable(
                name: "FlightStates");

            migrationBuilder.DropTable(
                name: "Missions");
        }
    }
}
