using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace FastDragon
{
    [JsonObject(MemberSerialization.OptIn)]
    public class LevelProgress
    {
        [JsonProperty] public HashSet<string> CollectedFairies = new();
        [JsonProperty] public Dictionary<GemColor, HashSet<string>> CollectedGems = new();
        [JsonProperty] public int SpentGems = 0;

        [JsonProperty] public bool ExitReached;

        /// <summary>
        /// Story flags that should persist even between level visits.
        /// If you want something that will reset when you re-visit the level,
        /// use <see cref="SaveFile.LevelVisit.StoryFlags"/> instead.
        /// </summary>
        [JsonProperty] public HashSet<string> StoryFlags = new();

        public int FairiesCollected => CollectedFairies.Count;
        public int TotalGemsCollected => CollectedGems.Sum(kvp => ((int)kvp.Key) * kvp.Value.Count);

        public void ResetProgress()
        {
            CollectedFairies.Clear();
            CollectedGems.Clear();
            SpentGems = 0;

            ExitReached = false;
            StoryFlags.Clear();
        }

        public void CollectGem(GemColor color, string saveKey)
        {
            if (!CollectedGems.TryGetValue(color, out var set))
            {
                set = new HashSet<string>();
                CollectedGems[color] = set;
            }

            set.Add(saveKey);
        }

        public bool IsGemCollected(GemColor color, string saveKey)
        {
            if (!CollectedGems.TryGetValue(color, out var set))
                return false;

            return set.Contains(saveKey);
        }
    }
}