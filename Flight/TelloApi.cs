// TelloApi
// The interface between Drone Navigator and the Tello drone, providing an idiomatic C# interface to the Tello API as
// documented by DJI. Future improvements are planned to decouple this class from the other components of the Flight
// sub-project so that more models of drones can be supported.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Windows.Media.Core;
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

    /// <summary>
    /// Receives state packets from the Tello over UDP and converts them to FlightStateModel objects. Events are
    /// provided that will fire every time a new state message is received. See <see cref="FlightStateChanged"/>.
    /// </summary>
    class TelloStateReceiver
    {
        private const int TelloStatePort = 8890;
        private readonly UdpClient _telloStateClient = new(TelloStatePort);
        private IPEndPoint _telloEndPoint;

        private FlightStateModel _flightState;

        public event FlightStateChangedCallback FlightStateChanged;

        CancellationTokenSource _quitting = new();
        public bool Connected = false;

        private readonly Thread _stateThread;

        public TelloStateReceiver(FlightStateChangedCallback updateCallback)
        {
            NetworkChange.NetworkAddressChanged += delegate
            {
                _telloEndPoint = GenerateTelloEndPoint();
            };
            _telloEndPoint = GenerateTelloEndPoint();
            _telloStateClient.Client.ReceiveTimeout = 500; // Milliseconds
            _stateThread = new Thread(new ThreadStart(ConstantlyReceiveState));
            _stateThread.Start();
            FlightStateChanged += updateCallback;
        }

        /// <summary>
        /// Reinitializes the Tello IP endpoint object. This should be called whenever a network connection change
        /// occurs, as new IP addresses may have been attained that can now communicate with the drone.
        /// </summary>
        /// <returns></returns>
        private static IPEndPoint GenerateTelloEndPoint()
        {
            return new IPEndPoint(IPAddress.Any, TelloStatePort);
        }

        /// <summary>
        /// Shuts down network connections and stops background threads. <see cref="TelloStateReceiver"/> cannot be
        /// reused after this method is called.
        /// </summary>
        public void Quit()
        {
            _quitting.Cancel();
            Connected = false;
            _stateThread.Join();
            _telloStateClient.Close();
        }

        /// <summary>
        /// Temporarily stop receiving state packets from a connected drone.
        /// </summary>
        public void Pause()
        {
            Connected = false;
        }

        /// <summary>
        /// Executed by <see cref="_stateThread"/>. Continuously receives state packets from the drone and converts
        /// them to FlightStateModel objects, then fires the <see cref="FlightStateChanged"/> event with the new state.
        /// </summary>
        public void ConstantlyReceiveState()
        {
            // Allow the thread to be quit by other parts of the system.
            while (!_quitting.IsCancellationRequested)
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

    /// <summary>
    /// Handles network connections and communication with the Tello drone.
    /// </summary>
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

        private FlightStateModel currentState;

        public bool Connected => currentState != null;

        public TelloApi()
        {
            // Initialize the state message receiver class.
            _telloStateReceiver = new TelloStateReceiver(StateUpdatedCallback);
            _commandResponseClient.Client.ReceiveTimeout = 5000;

            // Initialize the video receiver class.
            _telloVideoReceiver = new TelloVideoReceiver(VideoStreamPort);
        }

        internal void StateUpdatedCallback(FlightStateModel newState)
        {
            currentState = newState;
            OnFlightStatusChanged?.Invoke(this, new FlightStatusChangedEventArgs(currentState));
        }

        /// <summary>
        /// Shut down the entire Tello API object and release its resources. This ensures that network sockets are
        /// closed and any background threads are shut down.
        /// </summary>
        public void Quit()
        {
            currentState = null;
            _telloStateReceiver.Quit();
            _telloVideoReceiver.Quit();
            _commandResponseClient.Close();
        }

        /// <summary>
        /// Send a single string command to the Tello. This string will be encoded to UTF-8.
        /// </summary>
        /// <param name="command">The command to send to the Tello.</param>
        private void SendCommand(string command)
        {
            byte[] dgram = Encoding.UTF8.GetBytes(command);
            if(_commandResponseClient?.Client != null
                && _commandResponseClient.Client.Connected) {
                _commandResponseClient.Send(dgram, dgram.Length);
            }
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
            if(!Connected)
                return CommandResponse.Error;
            SendCommand(command);
            // Read the response string from the drone, convert it to a UTF-8 string, and trim trailing CRLF newlines
            string response = "";
            try
            {
                response = Encoding.UTF8.GetString(_commandResponseClient.Receive(ref _telloEndPoint))
                    .TrimEnd('\r', '\n');
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.TimedOut)
                    return CommandResponse.Error;
                throw;
            }

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
            // Initialize the _commandResponseClient
            _commandResponseClient.Connect(_telloEndPoint);

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

        /// <summary>
        /// Stops the connection to the drone temporarily, allowing it to be resumed with a call to
        /// <see cref="StartConnection"/>.
        /// </summary>
        public void StopConnection()
        {
            // Stop expecting state packets.
            _telloStateReceiver.Pause();
            // Clear the current flight state.
            currentState = null;
        }

        /// <summary>
        /// Send a single keepalive mesage to the drone to ensure it does not close the network connection.
        /// Currently unused.
        /// </summary>
        public void SendKeepAlive()
        {
            SendCommand("keepalive");
        }

        /// <summary>
        /// Send a command to the drone to begin taking off. Has no effect if the drone is already in flight.
        /// </summary>
        /// <returns>A <see cref="CommandResponse"/> indicating whether the command succeeded or failed.</returns>
        public CommandResponse Takeoff()
        {
            return SendCommandAndWaitForResponse("takeoff");
        }

        /// <summary>
        /// Send a command to the drone to begin automatically landing. Has no effect if the drone is already landed.
        /// </summary>
        /// <returns>A <see cref="CommandResponse"/> indicating whether the command succeeded or failed.</returns>
        public CommandResponse Land()
        {
            return SendCommandAndWaitForResponse("land");
        }

        /// <summary>
        /// Starts the background thread that receives video from the drone, and sends a command to the drone that it
        /// should begin streaming video.
        /// </summary>
        /// <returns>A <see cref="CommandResponse"/> indicating whether the command succeeded or failed.</returns>
        public CommandResponse StartVideo()
        {
            _telloVideoReceiver.Start();
            return SendCommandAndWaitForResponse("streamon");
        }

        /// <summary>
        /// Stops the background thread that receives video from the drone, and sends a command to the drone that it
        /// should stop streaming video.
        /// </summary>
        /// <returns>A <see cref="CommandResponse"/> indicating whether the command succeeded or failed.</returns>
        public ConcurrentQueue<MediaStreamSample> StopVideo()
        {
            var queue = _telloVideoReceiver.Stop();
            SendCommandAndWaitForResponse("streamoff");
            return queue;
        }

        /// <summary>
        /// Sends an RC command message to the drone, commanding the drone to move in 3D space.
        /// </summary>
        /// <param name="roll">Left-right movement of the drone, ranging from -100 to 100 respectively.</param>
        /// <param name="pitch">Backward-forward movement of the drone, ranging from -100 to 100 respectively.</param>
        /// <param name="throttle">Down-up movement of the drone, ranging from -100 to 100 respectively.</param>
        /// <param name="yaw">Left-right rotation of the drone, ranging from -100 to 100 respectively.</param>
        public void RcControl(int roll, int pitch, int throttle, int yaw)
        {
            SendCommand($"rc {roll:D} {pitch:D} {throttle:D} {yaw:D}");
        }
    }
}
