using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    [JsonObject(MemberSerialization.OptIn)]
    public partial class SaveFile : RefCounted
    {
        [JsonProperty] public int PlayerHealth = Player.MaxHealth;
        [JsonProperty] public string CurrentLevel;
        [JsonProperty] public string CurrentCheckpoint = null;

        /// <summary>
        /// The number of times the player has died outside of time trial mode.
        /// You don't get punished for this; it's just a fun little counter.
        ///
        /// Reloading a checkpoint from the pause menu counts as a death, btw.
        /// Otherwise, you'd be able to cheese it by pausing and reloading right
        /// before you die.
        /// </summary>
        [JsonProperty] public int TotalDeaths;

        [JsonProperty] public Dictionary<string, LevelSaveData> Levels = new();

        public int TotalGemsSpent => Levels.Values.Sum(l => l.Progress.SpentGems);
        public int TotalGemCount => Levels.Values.Sum(l => l.Progress.TotalGemsCollected) - TotalGemsSpent;
        public int TotalFairyCount => Levels.Values.Sum(l => l.Progress.CollectedFairies.Count);

        /// <summary>
        /// Data about your current visit to the level you're currently on.
        /// Used for showing stats at the end of the level.
        /// </summary>
        [JsonProperty] public LevelVisit CurrentLevelVisit = new();
        [JsonObject(MemberSerialization.OptIn)]
        public class LevelVisit
        {
            [JsonProperty] public int Deaths;
            [JsonProperty] public int FairiesFound;
            [JsonProperty] public int GemsSpent;
            [JsonProperty] public Dictionary<GemColor, int> GemsFound = new();

            public int TotalGemsFound => GemsFound.Sum(x => (int)x.Key * x.Value);

            public void AddToGemsFound(GemColor color)
            {
                if (!GemsFound.ContainsKey(color))
                    GemsFound[color] = 0;

                GemsFound[color]++;
            }
        }

        public static SaveFile FromJson(string json)
        {
            return JsonConvert.DeserializeObject<SaveFile>(json);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(
                this,
                new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                }
            );
        }

        public LevelSaveData GetLevelSaveData(string level)
        {
            if (!Levels.ContainsKey(level))
                Levels.Add(level, new LevelSaveData());

            return Levels[level];
        }

        public double GetPercentComplete(string levelSceneFile)
        {
            var levelSummary = AtlasCache.Instance.GetEntry(levelSceneFile);
            var progress = GetLevelSaveData(levelSceneFile).Progress;
            int categories = 0;
            double totalPercent = 0;

            if (levelSummary.TotalFairiesInLevel != 0)
            {
                categories++;
                totalPercent += ((double)progress.TotalGemsCollected) / levelSummary.TotalGemsInLevel;
            }

            if (levelSummary.TotalGemsInLevel != 0)
            {
                categories++;
                totalPercent += ((double)progress.CollectedFairies.Count) / levelSummary.TotalFairiesInLevel;
            }

            return categories == 0
                ? 1
                : (totalPercent / categories);
        }
    }
}