using System;

namespace FastDragon
{
    public static class TimeUtils
    {
        public static string FormatStopwatch(double seconds)
        {
            return TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.ff");
        }
    }
}