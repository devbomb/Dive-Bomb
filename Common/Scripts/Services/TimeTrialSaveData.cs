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

        [JsonIgnore] public string[] UnlockedLevels => Levels
            .Where(kvp => kvp.Value.SomethingUnlocked)
            .Where(kvp => ResourceLoader.Exists(kvp.Key)) // HACK: don't show levels that have been deleted
            .Select(kvp => kvp.Key)
            .ToArray();

        [JsonProperty] private Dictionary<string, LevelEntry> Levels = new Dictionary<string, LevelEntry>();
        private class LevelEntry : Dictionary<TimeTrialCategory, CategoryEntry>
        {
            [JsonIgnore] public bool SomethingUnlocked => Values.Any(c => c.Unlocked);
        }

        public class CategoryEntry
        {
            public bool Unlocked;
            public uint? BestTimePhysicsTicks;
        }

        public CategoryEntry GetEntry(string levelScenePath, TimeTrialCategory category)
        {
            if (!Levels.ContainsKey(levelScenePath))
                Levels[levelScenePath] = new LevelEntry();

            if (!Levels[levelScenePath].ContainsKey(category))
                Levels[levelScenePath][category] = new CategoryEntry();

            return Levels[levelScenePath][category];
        }

        public void UnlockCategory(string levelScenePath, TimeTrialCategory category)
        {
            GetEntry(levelScenePath, category).Unlocked = true;
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