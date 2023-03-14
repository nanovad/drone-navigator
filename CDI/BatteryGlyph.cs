using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CDI
{
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

        public static string? FromPercent(float percent)
        {
            // Round to the nearest 10%
            int closestPercent = (int)(Math.Round(percent / 100, 1) * 10);
            if(closestPercent < 0 || closestPercent > 10)
                return null;
            return batteryPercents[closestPercent];
        }
    }
}
