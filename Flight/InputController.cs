// InputController
// A background thread for processing information being received by a game controller and passing the pilot's inputs
// on to the drone in the proper format.
// This class is currently tightly coupled to the TelloApi class for simplicity, but a future improvement is planned to
// decouple these classes via an interface.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Gaming.Input;

namespace Flight
{
    /// <summary>
    /// A background thread for processing information being received by a game controller and passing the pilot's inputs
    /// on to the drone in the proper format.
    /// This class is currently tightly coupled to the TelloApi class for simplicity, but a future improvement is planned to
    /// decouple these classes via an interface.
    /// </summary>
    internal class InputController
    {
        private RawGameController? _controller;

        private bool _initialized = false;
        private readonly int _pollingRateHz;
        private Thread? _inputPollingThread;

        public bool[]? Buttons;
        public GameControllerSwitchPosition[]? Switches;
        public double[]? Axes;

        public TelloApi? TargetApi;

        CancellationTokenSource _quitting = new();

        public InputController(int pollingRateHz)
        {
            _pollingRateHz = pollingRateHz;
        }

        /// <summary>
        /// Searches for game controllers attached to the system.
        /// Likely will return false initially, as the system has not enumerated its own controllers yet.
        /// Therefore, this function should be called in a loop with a timeout to ensure no false negatives.
        /// This function also initializes the <see cref="_controller"/>, <see cref="Buttons"/>, <see cref="Switches"/>
        /// and <see cref="Axes"/> fields on successfully locating a controller.
        /// </summary>
        /// <returns>A boolean indicating whether or not controllers were found.</returns>
        private bool SearchForGameControllers()
        {
            var controllers = Windows.Gaming.Input.RawGameController.RawGameControllers;
            if (controllers.Count == 0)
                return false;
            _controller = controllers[0];

            Buttons = new bool[_controller.ButtonCount];
            Switches = new GameControllerSwitchPosition[_controller.SwitchCount];
            Axes = new double[_controller.AxisCount];

            return true;
        }

        /// <summary>
        /// Initializes the class and searches for a controller with a 10 second timeout. If no controller is found
        /// within the timeout, an exception is thrown.
        /// </summary>
        /// <exception cref="ControllerNotFoundException">Thrown when no suitable controller can be found.</exception>
        public void Initialize()
        {
            _inputPollingThread = new Thread(PerformInputPolling);
            _quitting = new();

            var timeout = Task.Delay(TimeSpan.FromSeconds(10));

            while (!_initialized && !timeout.IsCompleted)
                _initialized = SearchForGameControllers();

            if (_controller is null)
                throw new ControllerNotFoundException("Unable to find a suitable controller");
        }

        /// <summary>
        /// Starts the input polling thread. Ensure <see cref="Initialize"/> has been called before calling this.
        /// </summary>
        public void BeginInputPolling()
        {
            _inputPollingThread!.Start();
        }

        /// <summary>
        /// Shuts down the input polling thread and releases its resources. Takes no action if the thread is already
        /// stopped.
        /// </summary>
        public void EndInputPolling()
        {
            _quitting.Cancel();
            _inputPollingThread?.Interrupt();
            if(_inputPollingThread?.IsAlive ?? false)
                _inputPollingThread.Join();
        }

        /// <summary>
        /// Continuously monitor the game controller for inputs, remapping them to RC control commands for the drone,
        /// and send them directly to the drone via TelloApi.
        /// This method should only be executed from <see cref="_inputPollingThread"/>.
        /// </summary>
        private async void PerformInputPolling()
        {
            // Convert the polling rate from Hz to a millisecond interval.
            int pollingRateLimitInterval = 1000 / _pollingRateHz;
            try
            {
                // Ensure the thread has not been requested to stop.
                while (!_quitting.IsCancellationRequested)
                {
                    // Set up an asynchronous task to ensure the loop does not run at a higher polling rate than
                    // requested. This task is set up to fire a number of milliseconds in the future. If reading the
                    // commands from the controller and transmitting the RC commands to the drone occurs faster than
                    // the rate limit specifies, it will wait to ensure the loop takes no less than
                    // pollingRateLimitInterval milliseconds. If it takes longer, the delay will have no effect.
                    var rateLimit = Task.Delay(TimeSpan.FromMilliseconds(pollingRateLimitInterval));

                    // Read the current state of the controller.
                    _controller?.GetCurrentReading(Buttons, Switches, Axes);

                    // Convert the values from the axis readings, which are from the range of 0 to 1, with 0.5 being 
                    // the center, to RC control commands, which expect a range of -100 to 100, per the Tello spec.
                    TargetApi?.RcControl(
                        (int)((Axes[2] - 0.5) * 200),
                        (int)((Axes[5] - 0.5) * 200 * -1),
                        (int)((Axes[1] - 0.5) * 200 * -1),
                        (int)((Axes[0] - 0.5) * 200));

                    // Wait for the rate limiting task if it has not yet completed.
                    await rateLimit;
                }
            }
            catch (ThreadInterruptedException e)
            {
                // No extra shutdown or resource release is necessary if the thread is quitting.
            }
        }
    }
}
