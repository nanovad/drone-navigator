// ControllerNotFoundException
// A custom Exception class that is thrown when the controller cannot be found during the initialization of the Flight
// systems.

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

namespace Flight
{
    /// <summary>
    /// A custom Exception thrown when the controller cannot be found during the initialization of Flight systems.
    /// </summary>
    internal class ControllerNotFoundException : Exception
    {
        public ControllerNotFoundException() : base() { }
        public ControllerNotFoundException(string message) : base(message) { }
        public ControllerNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
