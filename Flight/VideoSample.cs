using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace Flight
{
    internal class VideoSample
    {
        public TimeSpan Start;
        public TimeSpan Duration;
        public byte[] Raw;

        public VideoSample(TimeSpan start, TimeSpan duration, byte[] raw) { Start = start; Duration = duration; Raw = raw; }
    }
}
