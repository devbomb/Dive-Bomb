using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    public class TimeTrialSaveData
    {
        private const string FilePath = "user://TimeTrialSaveData.json";

        public static TimeTrialSaveData Instance { get; } = LoadFromJson();

        [JsonIgnore] public string[] UnlockedMaps => Maps
            .Where(kvp => kvp.Value.SomethingUnlocked)
            .Where(kvp => ResourceLoader.Exists(kvp.Key)) // HACK: don't show levels that have been deleted
            .Select(kvp => kvp.Key)
            .ToArray();

        [JsonProperty] private Dictionary<string, MapEntry> Maps = new Dictionary<string, MapEntry>();
        private class MapEntry : Dictionary<TimeTrialCategory, CategoryEntry>
        {
            [JsonIgnore] public bool SomethingUnlocked => Values.Any(c => c.Unlocked);
        }

        public class CategoryEntry
        {
            public bool Unlocked;
            public uint? BestTimePhysicsTicks;
        }

        public CategoryEntry GetEntry(string mapFilePath, TimeTrialCategory category)
        {
            if (!Maps.ContainsKey(mapFilePath))
                Maps[mapFilePath] = new MapEntry();

            if (!Maps[mapFilePath].ContainsKey(category))
                Maps[mapFilePath][category] = new CategoryEntry();

            return Maps[mapFilePath][category];
        }

        public void UnlockCategory(string mapFilePath, TimeTrialCategory category)
        {
            GetEntry(mapFilePath, category).Unlocked = true;
            SaveToJson();
        }

        public void SaveToJson()
        {
            string json = JsonConvert.SerializeObject(
                this,
                new JsonSerializerSettings { Formatting = Formatting.Indented }
            );

            using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
            file.StoreLine(json);
            file.Close();
        }

        private static TimeTrialSaveData LoadFromJson()
        {
            if (!FileAccess.FileExists(FilePath))
                return new TimeTrialSaveData();

            try
            {
                using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                file.Close();

                return JsonConvert.DeserializeObject<TimeTrialSaveData>(json);
            }
            catch (JsonException err)
            {
                GD.PushWarning($"Error parsing TimeTrialSaveData.  Wiping progress.\n{err}");
                return new TimeTrialSaveData();
            }
        }
    }
}