using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Flight
{
    internal class ControllerNotFoundException : Exception
    {
        public ControllerNotFoundException() : base() { }
        public ControllerNotFoundException(string message) : base(message) { }
        public ControllerNotFoundException(string message, Exception innerException) : base(message, innerException) { }
    }
}
