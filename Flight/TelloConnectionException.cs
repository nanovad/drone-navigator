// TelloConnectionException
// A custom Exception that is thrown when a connection attempt to a Tello drone fails.

// By Nicholas De Nova
// For CPSC-4900 Senior Project & Seminar
// With Professor Freddie Kato
// At Governors State University
// In Spring 2023

using System;

namespace Flight
{
    [Serializable]
    internal class TelloConnectionException : Exception
    {
        public TelloConnectionException() { }
        public TelloConnectionException(string message) : base(message) { }
        public TelloConnectionException(string message, Exception innerException) : base(message, innerException) { }
    }
}