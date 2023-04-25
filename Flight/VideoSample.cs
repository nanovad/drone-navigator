// VideoSample
// A data class for holding individual samples of video data as they are received from the drone over the network.

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
using Windows.Media.Core;

namespace Flight
{
    /// <summary>
    /// Holds individual samples of video data as they are received from the drone over the network.
    /// </summary>
    internal class VideoSample
    {
        public TimeSpan Start;
        public TimeSpan Duration;
        public byte[] Raw;

        public VideoSample(TimeSpan start, TimeSpan duration, byte[] raw) { Start = start; Duration = duration; Raw = raw; }
    }
}
