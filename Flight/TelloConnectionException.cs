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