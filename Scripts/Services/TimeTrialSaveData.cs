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

        [JsonIgnore] public string[] UnlockedMaps => Entries
            .Where(kvp => kvp.Value.AnyPercentUnlocked)
            .Select(kvp => kvp.Key)
            .ToArray();

        [JsonProperty] private Dictionary<string, Entry> Entries = new Dictionary<string, Entry>();
        public class Entry
        {
            public bool AnyPercentUnlocked = false;
            public double? AnyPercentRecord = null;
        }

        public Entry GetEntry(string mapFilePath)
        {
            if (!Entries.ContainsKey(mapFilePath))
            {
                Entries[mapFilePath] = new Entry();
            }

            return Entries[mapFilePath];
        }

        public void UnlockAnyPercent(string mapFilePath)
        {
            GetEntry(mapFilePath).AnyPercentUnlocked = true;
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