// IFlightStatusProvider.cs
// A generic interface for classes streaming flight status data updates via callbacks.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

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
        /// <summary>
        /// A delegate to be called when the flight status that the implementing class holds changes.
        /// </summary>
        /// <param name="sender">The class where the change originated.</param>
        /// <param name="e">
        /// The <see cref="FlightStatusChangedEventArgs"/> containing the newly changed flight state model object.
        /// </param>
        public delegate void OnFlightStatusChangedHandler(object sender, FlightStatusChangedEventArgs e);
        /// <summary>
        ///  The event that is fired when the flight state is changed.
        /// </summary>
        public event OnFlightStatusChangedHandler OnFlightStatusChanged;
    }
}
