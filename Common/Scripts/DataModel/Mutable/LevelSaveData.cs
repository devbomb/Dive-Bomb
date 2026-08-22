using System;
using System.Collections.Generic;

namespace FastDragon
{
    public class LevelSaveData
    {
        public bool VisitedOnce = false;
        public LevelProgress Progress { get; set; } = new();
        public Dictionary<TimeTrialCategory, PhysicsTicks> TimeTrialBestTime = new();
    }
}