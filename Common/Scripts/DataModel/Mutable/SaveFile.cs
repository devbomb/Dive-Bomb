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

        [JsonProperty] public int UntalliedGemsSpent;
        [JsonProperty] public Dictionary<GemColor, int> UntalliedGemsCollected = new();

        [JsonProperty] public Dictionary<string, LevelSaveData> Levels = new();

        public int TotalGemsSpent => Levels.Values.Sum(l => l.Progress.SpentGems);
        public int TotalGemCount => Levels.Values.Sum(l => l.Progress.TotalGemsCollected) - TotalGemsSpent;
        public int TotalFairyCount => Levels.Values.Sum(l => l.Progress.CollectedFairies.Count);

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