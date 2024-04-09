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

        public SparxColor PlayerHealth = SparxColor.Gold;
        public string CurrentMap;
        public string CurrentCheckpoint = null;

        public HashSet<string> CollectedGems = new HashSet<string>();

        public Dictionary<GemColor, int> UntalliedGems = new Dictionary<GemColor, int>();

        public Dictionary<string, MapProgress> Maps = new Dictionary<string, MapProgress>();
        public class MapProgress
        {
            public int GemsCollected = 0;
            public HashSet<string> CollectedFairies = new HashSet<string>();
        }

        [JsonIgnore] public int TotalGemCount => Maps.Values.Sum(l => l.GemsCollected);
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
                totalPercent += ((double)progress.GemsCollected) / cacheEntry.TotalGemsInLevel;
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

        public bool IsGemCollected(string nodePath)
        {
            return CollectedGems.Contains(nodePath);
        }
    }
}