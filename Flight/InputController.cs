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
        private readonly Thread _inputPollingThread;

        public bool[]? Buttons;
        public GameControllerSwitchPosition[]? Switches;
        public double[]? Axes;

        public TelloApi? TargetApi;

        public InputController(int pollingRateHz)
        {
            _inputPollingThread = new Thread(PerformInputPolling);
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
            var timeout = Task.Delay(TimeSpan.FromSeconds(10));

            while (!_initialized && !timeout.IsCompleted)
                _initialized = SearchForGameControllers();

            if (_controller is null)
                throw new ControllerNotFoundException("Unable to find a suitable controller");
        }

        public void BeginInputPolling()
        {
            _inputPollingThread.Start();
        }

        public void EndInputPolling()
        {
            _inputPollingThread.Interrupt();
        }

        private async void PerformInputPolling()
        {
            int pollingRateLimitInterval = 1000 / _pollingRateHz;
            try
            {
                while (true)
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
