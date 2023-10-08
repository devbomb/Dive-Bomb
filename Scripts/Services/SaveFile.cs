using System;
using System.Collections.Generic;

namespace FastDragon
{
    public class SaveFile
    {
        public static SaveFile Current = new SaveFile();

        public int TotalGemCount = 0;
        public string CurrentMap;
        public HashSet<string> CollectedGems = new HashSet<string>();

        public static void Reset()
        {
            Current = new SaveFile();
        }

        public bool IsGemCollected(string nodePath)
        {
            return CollectedGems.Contains(nodePath);
        }
    }
}