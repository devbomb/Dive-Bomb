using System;
using Godot;

namespace FastDragon
{
    public static class TimeUtils
    {
        public static string FormatPhysicsTicksStopwatch(uint ticks)
        {
            double seconds = ((double)ticks) / Engine.PhysicsTicksPerSecond;
            return TimeSpan.FromSeconds(seconds).ToString(@"mm\:ss\.ff");
        }
    }
}