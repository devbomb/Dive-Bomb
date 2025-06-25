using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    public partial class SaveFile : Resource
    {
        public static SaveFile Current = new SaveFile();

        public int PlayerHealth = Player.MaxHealth;
        public string CurrentMap;
        public string CurrentCheckpoint = null;

        public int GemsSpent;
        public int UntalliedGemsSpent;
        public Dictionary<GemColor, int> UntalliedGemsCollected = new Dictionary<GemColor, int>();

        public Dictionary<string, MapProgress> Maps = new Dictionary<string, MapProgress>();
        public class MapProgress
        {
            [JsonIgnore] public int FairiesCollected => CollectedFairies.Count;
            [JsonIgnore] public int TotalGemsCollected => CollectedGems.Sum(kvp => ((int)kvp.Key) * kvp.Value.Count);

            public HashSet<string> CollectedFairies = new();
            public Dictionary<GemColor, HashSet<string>> CollectedGems = new();

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

        [JsonIgnore] public int TotalGemCount => Maps.Values.Sum(l => l.TotalGemsCollected) - GemsSpent;
        [JsonIgnore] public int TotalFairyCount => Maps.Values.Sum(l => l.CollectedFairies.Count);
        [JsonIgnore] public MapProgress CurrentMapProgress => GetMapProgress(CurrentMap);

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

        public bool IsGemCollected(string map, GemColor color, string saveKey)
        {
            return GetMapProgress(map).IsGemCollected(color, saveKey);
        }

        public void CollectGem(string map, GemColor color, string saveKey)
        {
            GetMapProgress(map).CollectGem(color, saveKey);

            if (!UntalliedGemsCollected.ContainsKey(color))
            {
                UntalliedGemsCollected[color] = 0;
            }

            UntalliedGemsCollected[color]++;
        }

        public void SpendGems(int amount)
        {
            GemsSpent += amount;
            UntalliedGemsSpent += amount;
        }

        public void CollectFairy(string map, string saveKey)
        {
            var progress = GetMapProgress(map);
            progress.CollectedFairies.Add(saveKey);
        }

        public bool IsFairyCollected(string map, string saveKey)
        {
            var progress = GetMapProgress(map);
            return progress.CollectedFairies.Contains(saveKey);
        }

        public MapProgress GetMapProgress(string map)
        {
            if (!Maps.ContainsKey(map))
                Maps.Add(map, new MapProgress());

            return Maps[map];
        }

        public double GetPercentComplete(string mapFilePath)
        {
            var cacheEntry = AtlasCache.Instance.GetEntry(mapFilePath);
            var progress = GetMapProgress(mapFilePath);
            int categories = 0;
            double totalPercent = 0;

            if (cacheEntry.TotalFairiesInLevel != 0)
            {
                categories++;
                totalPercent += ((double)progress.TotalGemsCollected) / cacheEntry.TotalGemsInLevel;
            }

            if (cacheEntry.TotalGemsInLevel != 0)
            {
                categories++;
                totalPercent += ((double)progress.CollectedFairies.Count) / cacheEntry.TotalFairiesInLevel;
            }

            return categories == 0
                ? 1
                : (totalPercent / categories);
        }
    }
}