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
        public static SaveFile Current = new SaveFile();

        [JsonProperty] public int PlayerHealth = Player.MaxHealth;
        [JsonProperty] public string CurrentLevel;
        [JsonProperty] public string CurrentCheckpoint = null;

        [JsonProperty] public int UntalliedGemsSpent;
        [JsonProperty] public Dictionary<GemColor, int> UntalliedGemsCollected = new();

        [JsonProperty] public Dictionary<string, LevelProgress> Levels = new();

        public int TotalGemsSpent => Levels.Values.Sum(l => l.SpentGems);
        public int TotalGemCount => Levels.Values.Sum(l => l.TotalGemsCollected) - TotalGemsSpent;
        public int TotalFairyCount => Levels.Values.Sum(l => l.CollectedFairies.Count);
        public LevelProgress CurrentLevelProgress => GetLevelProgress(CurrentLevel);

        public static void Reset()
        {
            Current = new SaveFile();
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

        public void AddUntalliedGem(GemColor color)
        {
            if (!UntalliedGemsCollected.ContainsKey(color))
            {
                UntalliedGemsCollected[color] = 0;
            }

            UntalliedGemsCollected[color]++;
        }

        public LevelProgress GetLevelProgress(string level)
        {
            if (!Levels.ContainsKey(level))
                Levels.Add(level, new LevelProgress());

            return Levels[level];
        }

        public double GetPercentComplete(string levelSceneFile)
        {
            var levelSummary = AtlasCache.Instance.GetEntry(levelSceneFile);
            var progress = GetLevelProgress(levelSceneFile);
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