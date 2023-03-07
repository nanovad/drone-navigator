using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using CDI;
using FlightDataModel;

namespace Flight
{
    internal delegate void FlightStateChangedCallback(FlightStateModel newState);

    enum CommandResponse
    {
        Ok,
        Error,
    }

    class TelloStateReceiver
    {
        private const int TelloStatePort = 8890;
        private readonly UdpClient _telloStateClient = new(TelloStatePort);
        private IPEndPoint _telloEndPoint;

        private FlightStateModel _flightState;

        public event FlightStateChangedCallback FlightStateChanged;

        private volatile bool _quitting = false;
        public bool Connected = false;

        public TelloStateReceiver(IPEndPoint telloEndPoint, FlightStateChangedCallback updateCallback)
        {
            _telloEndPoint = new IPEndPoint(IPAddress.Any, TelloStatePort);
            _telloStateClient.Client.ReceiveTimeout = 500; // Milliseconds
            FlightStateChanged += updateCallback;
        }

        public void Quit()
        {
            _quitting = true;
            Connected = false;
        }

        public void ConstantlyReceiveState()
        {
            while (!_quitting)
            {
                if (!Connected)
                {
                    // Busy wait to connect
                    Thread.Sleep(50);
                    continue;
                }

                byte[] telloStateBytes;
                try
                {
                    // Receive a state string from the Tello as it becomes available
                    telloStateBytes = _telloStateClient.Receive(ref _telloEndPoint);
                }
                catch (SocketException e)
                {
                    if (e.SocketErrorCode == SocketError.TimedOut)
                    {
                        // This is expected - we may encounter a few timeouts during the initial connection phase.
                        continue;
                    }

                    // If it's a SocketException for any other reason, it's an error. Rethrow it.
                    throw;
                }

                // Decode it from UTF-8 bytes
                string state = Encoding.UTF8.GetString(telloStateBytes);

                FlightStateModel newState = ParseFlightState(state);
                if (newState != _flightState)
                {
                    // Something about the flight state changed - fire the callback
                    _flightState = newState;
                    FlightStateChanged?.Invoke(_flightState);
                }

            }
        }

        /// <summary>
        /// Parse a valid FlightState from a complete, raw (but already bytes-decoded) state string from the Tello.
        /// </summary>
        /// <param name="state">A string that represents the flight state in the Tello protocol format</param>
        /// <returns>A new FlightStateModel populated with data parsed from the <paramref name="state"/> parameter.</returns>
        private FlightStateModel ParseFlightState(string state)
        {
            FlightStateModel current = new();

            // Trim newline characters from the end of the state string.
            state = state.TrimEnd('\r', '\n');
            // Trim trailing field termination character.
            state = state.TrimEnd(';');

            // Parse the state string
            // example string: mid:%d;x:%d;y:%d;z:%d;mpry:%d,%d,%d;pitch:%d;roll:%d;yaw:%d;vgx:%d;vgy%d;v gz:% d;templ:%d;temph:%d;tof:%d;h:%d;bat:%d;baro:%f;\r\n
            Dictionary<string, string> kvps = new();
            string[] fields = state.Split(';');
            foreach (string field in fields)
            {
                string[] pair = field.Split(':');

                // mid, x, y, z, and mpry are ignored, as they are mission pad related and we don't support those.
                try
                {
                    kvps.Add(pair[0], pair[1]);
                }
                catch
                {
                    // Silently ignore - maybe a malformed state string?
                    // TODO: Log somewhere?
                }
            }

            try
            {
                // In units of degrees.
                current.Pitch = Convert.ToSingle(kvps["pitch"]);
                current.Roll = Convert.ToSingle(kvps["roll"]);
                current.Yaw = Convert.ToSingle(kvps["yaw"]);
            }
            catch
            {
                // TODO: Log somewhere.
            }

            try
            {
                // In units of dm/s (decimeters per second).
                current.Vgx = Convert.ToSingle(kvps["vgx"]);
                current.Vgy = Convert.ToSingle(kvps["vgy"]);
                current.Vgz = Convert.ToSingle(kvps["vgz"]);
            }
            catch
            {
                // TODO: Log somewhere.
            }

            try
            {
                // In units of cm/s^2.
                current.Agx = Convert.ToSingle(kvps["agx"]);
                current.Agy = Convert.ToSingle(kvps["agy"]);
                current.Agz = Convert.ToSingle(kvps["agz"]);
            }
            catch
            {
                // TODO: Log somewhere.
            }

            try
            {
                // In units of cm.
                current.TotalDistance = Convert.ToSingle(kvps["tof"]);
                current.Altitude = Convert.ToSingle(kvps["h"]);
                current.BarometricAltitude = Convert.ToSingle(kvps["baro"]);
                current.BatteryPercent = Convert.ToSingle(kvps["bat"]);
                // TODO: Derive current.TimeLeft with a regression analysis.
            }
            catch
            {
                // TODO: Log somewhere.
            }

            return current;
        }
    }

    internal class TelloApi : IFlightStatusProvider
    {
        public event IFlightStatusProvider.OnFlightStatusChangedHandler OnFlightStatusChanged;

        private const int CommandResponsePort = 8889;
        private const int VideoStreamPort = 11111;

        private readonly TimeSpan _telloInitialStatePacketTimeout = TimeSpan.FromMilliseconds(10000);

        private IPEndPoint _telloEndPoint = IPEndPoint.Parse("192.168.10.1:8889");
        public IPEndPoint TelloEndPoint => _telloEndPoint;

        private readonly UdpClient _commandResponseClient = new(CommandResponsePort);
        //private readonly UdpClient _videoStreamClient = new(VideoStreamPort);

        private readonly TelloStateReceiver _telloStateReceiver;
        private readonly TelloVideoReceiver _telloVideoReceiver;
        public TelloVideoReceiver VideoReceiver => _telloVideoReceiver;
        private readonly Thread _stateThread;

        private FlightStateModel currentState;

        public bool Connected => currentState != null;

        public TelloApi()
        {
            _telloStateReceiver = new TelloStateReceiver(_telloEndPoint, StateUpdatedCallback);
            _stateThread = new Thread(new ThreadStart(_telloStateReceiver.ConstantlyReceiveState));
            _stateThread.Start();
            _commandResponseClient.Connect(_telloEndPoint);

            _telloVideoReceiver = new TelloVideoReceiver(VideoStreamPort);
        }

        internal void StateUpdatedCallback(FlightStateModel newState)
        {
            currentState = newState;
            OnFlightStatusChanged?.Invoke(this, new FlightStatusChangedEventArgs(currentState));
        }

        /// <summary>
        /// Send a single string command to the Tello. This string will be encoded to UTF-8.
        /// </summary>
        /// <param name="command">The command to send to the Tello.</param>
        private void SendCommand(string command)
        {
            byte[] dgram = Encoding.UTF8.GetBytes(command);
            _commandResponseClient.Send(dgram, dgram.Length);
        }

        /// <summary>
        /// Send a single string command to the Tello with <see cref="SendCommand"/>, then waits to receive a response.
        /// </summary>
        /// <param name="command">The command to send to the Tello.</param>
        /// <returns>
        /// An instance of the CommandResponse enum indicating whether the command was successful or not.
        /// </returns>
        private CommandResponse SendCommandAndWaitForResponse(string command)
        {
            SendCommand(command);
            // Read the response string from the drone, convert it to a UTF-8 string, and trim trailing CRLF newlines
            string response = Encoding.UTF8.GetString(_commandResponseClient.Receive(ref _telloEndPoint))
                .TrimEnd('\r', '\n');
            return response == "ok" ? CommandResponse.Ok : CommandResponse.Error;
        }

        /// <summary>
        /// Attempts to connect to the Tello. Blocks until an initial state packet is received from the Tello or the
        /// connection timeout (as defined by <see cref="_telloInitialStatePacketTimeout"/>) is reached.
        /// </summary>
        /// <exception cref="TelloConnectionException">
        /// Thrown if the Tello did not begin sending state packets indicating that it is connected
        /// within the expected timeout interval.
        /// </exception>
        public void StartConnection()
        {
            // Clear the current state - we will need to check for it getting updated to determine whether or not the
            // Tello is alive and connected.
            currentState = null;

            // Send the SDK initialization command to the Tello.
            SendCommand("command");
            // (It never responds anything directly.)

            // Enable receiving state packets.
            _telloStateReceiver.Connected = true;

            // Wait to see if we get a state packet back from the Tello.
            // TODO: Make this async with callbacks instead of a busy wait / slow poll.
            DateTime timeoutTime = DateTime.Now.Add(_telloInitialStatePacketTimeout);
            while (DateTime.Now < timeoutTime)
            {
                if (currentState != null) break;
                // Small delay to reduce CPU consumption
                Thread.Sleep(10);
            }

            // If we still haven't received a state packet from the Tello by the timeout...
            if (currentState == null)
            {
                throw new TelloConnectionException(
                    "Tello did not respond with a state packet within timeout " +
                    _telloInitialStatePacketTimeout);
            }

            // If we haven't throw an exception by this point, the Tello has responded with a state packet.
            // We are connected!
        }

        public void StopConnection()
        {
            // Stop expecting state packets.
            _telloStateReceiver.Quit();
            // Clear the current flight state.
            currentState = null;
        }

        public void SendKeepAlive()
        {
            SendCommand("keepalive");
        }

        public CommandResponse Takeoff()
        {
            return SendCommandAndWaitForResponse("takeoff");
        }

        public CommandResponse Land()
        {
            return SendCommandAndWaitForResponse("land");
        }

        public CommandResponse StartVideo()
        {
            _telloVideoReceiver.Start();
            return SendCommandAndWaitForResponse("streamon");
        }

        public CommandResponse StopVideo()
        {
            _telloVideoReceiver.Stop();
            return SendCommandAndWaitForResponse("streamoff");
        }
    }
}
