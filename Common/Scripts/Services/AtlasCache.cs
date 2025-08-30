using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    /// <summary>
    /// Contains "static" data about levels that we would want to know without
    /// loading the whole level---such as its name, the total number of gems,
    /// etc.
    ///
    /// This information is recalcualted at runtime whenever the level is loaded,
    /// and then cached in a json file.
    ///
    /// Why do this at runtime?  Well...
    /// * To know how many collectables are in a level, we need to count them
    /// * Counting them manually while designing the level is absurd
    /// * Counting them automatically requires loading the whole level, so we
    ///     can't do this when the player opens the Atlas.  That would result
    ///     in a rediculous loading screen
    /// * We COULD count them automatically during the build or import process,
    ///     but I don't want to maintain a custom build process.  I'm already
    ///     pushing it with my custom .map file importer!
    ///
    /// So, a compromise was made: since the Atlas only shows you levels that
    /// you've already visited, we can count the collectables when you first
    /// visit that level(since it's already loaded at that point) and then cache
    /// it so the info can be seen when the level isn't loaded.
    /// </summary>
    public class AtlasCache
    {
        private const string FilePath = "user://AtlasCache.json";

        public static AtlasCache Instance { get; } = LoadFromJson();

        [JsonProperty]
        private Dictionary<string, Entry> Levels = new Dictionary<string, Entry>();
        public class Entry
        {
            public string HumanReadableName;
            public int TotalGemsInLevel;
            public int TotalFairiesInLevel;
        }

        public void UpdateCache(string levelSceneFile, Node levelRoot)
        {
            Levels[levelSceneFile] = new Entry
            {
                HumanReadableName = levelRoot.FindNode<Player>()?.LevelName
                    ?? "No level name specified, or scene does not have a Player",

                TotalGemsInLevel = levelRoot
                    .EnumerateDescendantsOfType<Gem>()
                    .Sum(g => (int)g.Value),

                TotalFairiesInLevel = levelRoot
                    .EnumerateDescendantsOfType<Fairy>()
                    .Count()
            };

            SaveToJson();
        }

        public Entry GetEntry(string levelSceneFile)
        {
            // If the player moves their save file to a different computer, then
            // they may have levels in their save file that aren't in the new
            // computer's cache.  If that happens, sneakily load the level and
            // update the cache.  This will be slow, but is expected to be rare.
            if (!Levels.ContainsKey(levelSceneFile))
            {
                GD.PushWarning($"Atlas cache miss: {levelSceneFile}");

                var levelRoot = ResourceLoader
                    .Load<PackedScene>(levelSceneFile)
                    .Instantiate<Node>();

                UpdateCache(levelSceneFile, levelRoot);

                levelRoot.Free();
            }

            return Levels[levelSceneFile];
        }

        private void SaveToJson()
        {
            string json = JsonConvert.SerializeObject(
                this,
                new JsonSerializerSettings { Formatting = Formatting.Indented }
            );

            using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
            file.StoreLine(json);
            file.Close();
        }

        private static AtlasCache LoadFromJson()
        {
            if (!FileAccess.FileExists(FilePath))
                return new AtlasCache();

            try
            {
                using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                file.Close();

                return JsonConvert.DeserializeObject<AtlasCache>(json);
            }
            catch (JsonException err)
            {
                GD.PushWarning($"Error parsing Atlas cache.  It will be regenerated.\n{err}");
                return new AtlasCache();
            }
        }
    }
}