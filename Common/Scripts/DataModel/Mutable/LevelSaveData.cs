using System;
using System.Collections.Generic;

namespace FastDragon
{
    public class LevelSaveData
    {
        public LevelProgress Progress { get; set; } = new();

        public Dictionary<TimeTrialCategory, PhysicsTicks> TimeTrialBestTimePhysicsTicks = new();
    }
}