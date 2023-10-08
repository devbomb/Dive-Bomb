using System;
using System.Collections.Generic;
using Godot;
using Newtonsoft.Json;

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

        public static void SaveTo(string filePath)
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Write);
            string json = JsonConvert.SerializeObject(Current);
            file.StoreLine(json);
            file.Close();
        }

        public static void LoadFrom(string filePath)
        {
            using var file = FileAccess.Open(filePath, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            Current = JsonConvert.DeserializeObject<SaveFile>(json);
            file.Close();

            // TODO: Is this the best place to put this?
            MapTransitionManager.Instance.GoToMap(Current.CurrentMap);
        }

        public bool IsGemCollected(string nodePath)
        {
            return CollectedGems.Contains(nodePath);
        }
    }
}