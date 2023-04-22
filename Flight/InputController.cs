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

        public void BeginInputPolling()
        {
            _inputPollingThread!.Start();
        }

        public void EndInputPolling()
        {
            _quitting.Cancel();
            _inputPollingThread?.Interrupt();
            if(_inputPollingThread?.IsAlive ?? false)
                _inputPollingThread.Join();
        }

        private async void PerformInputPolling()
        {
            int pollingRateLimitInterval = 1000 / _pollingRateHz;
            try
            {
                while (!_quitting.IsCancellationRequested)
                {
                    var rateLimit = Task.Delay(TimeSpan.FromMilliseconds(pollingRateLimitInterval));

                    _controller?.GetCurrentReading(Buttons, Switches, Axes);

                    TargetApi?.RcControl(
                        (int)((Axes[2] - 0.5) * 200),
                        (int)((Axes[5] - 0.5) * 200 * -1),
                        (int)((Axes[1] - 0.5) * 200 * -1),
                        (int)((Axes[0] - 0.5) * 200));

                    await rateLimit;
                }
            }
            catch (ThreadInterruptedException e)
            {

            }
        }
    }
}
