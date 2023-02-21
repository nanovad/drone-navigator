using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDI
{
    /// <summary>
    /// Provides a generic interface for classes that provide flight status data flowing from a source - this may be
    /// a replay data provider or a flight provider, like TelloApi (which itself is an IFlightStatusProvider, as it
    /// generates FlightState data).
    /// </summary>
    public interface IFlightStatusProvider
    {
        public delegate void OnFlightStatusChangedHandler(object sender, FlightStatusChangedEventArgs e);
        public event OnFlightStatusChangedHandler OnFlightStatusChanged;
    }
}
