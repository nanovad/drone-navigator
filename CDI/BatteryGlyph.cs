// BatteryGlyph
// A helper class that maps battery percentages to the appropriate symbol in the Segoe MDL2 Assets font.

// Inputs: The battery percentage (passed from CDI)
// Process: Mapping the battery percentage to the corresponding icon glyph
// Outputs: The icon glyph

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
    /// A small helper class for converting battery percentages to their
    /// corresponding icons in the Segoe MDL2 assets font.
    /// </summary>
    internal static class BatteryGlyph
    {
        static string[] batteryPercents = {
            "\xE850", // 0%
            "\xE851", // 10%
            "\xE852", // 20%
            "\xE853", // 30%
            "\xE854", // 40%
            "\xE855", // 50%
            "\xE856", // 60%
            "\xE857", // 70%
            "\xE858", // 80%
            "\xE859", // 90%
            "\xE83F"  // 100%
        };

        /// <summary>
        /// Converts a battery percentage, <paramref name="percent"/>, to the nearest (according to Math.Round)
        /// battery symbol, with a granularity of 10%.
        /// </summary>
        /// <param name="percent">
        /// The percentage of battery to convert to a symbol, between 0 and 100.
        /// </param>
        /// <returns>A string corresponding to the glyph in the Segoe MDL2 Assets font</returns>
        public static string? FromPercent(float percent)
        {
            // Round the battery percentage to the nearest 10%, by first dividing it to its coefficient representation,
            // rounding it to 1 decimal place, then multiplying it back to per-tenths.
            // Finally, cast it to an int, where it becomes an index to the batteryPercents array.
            int closestPercent = (int)(Math.Round(percent / 100, 1) * 10);
            // These would cause out-of-bounds accesses.
            if(closestPercent < 0 || closestPercent > 10)
                return null;
            return batteryPercents[closestPercent];
        }
    }
}
