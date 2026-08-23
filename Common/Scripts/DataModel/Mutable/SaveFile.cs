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
        /// <summary>
        ///     The format version used by save files written by this version of
        ///     the game.
        ///
        ///     Bump this whenever new stuff is added the save file that cannot
        ///     be read by older version of the game.
        /// </summary>
        public const int CurrentSaveFormatVersion = 1;

        /// <summary>
        ///     The oldest format version that can be parsed without data loss.
        ///
        ///     Bump this to <see cref="CurrentSaveFormatVersion"/> if the save
        ///     format has changed so much that old files cannot be parsed.
        /// </summary>
        public const int MinSaveFormatVersion = 1;

        [JsonProperty] public int? SaveFormatVersion = CurrentSaveFormatVersion;

        public LevelManifest CurrentLevel;
        [JsonProperty("CurrentLevel")] private string _currentLevelFilePath
        {
            get => CurrentLevel?.ResourcePath;
            set => CurrentLevel = value == null
                ? null
                : ResourceLoader.Load<LevelManifest>(value);
        }

        /// <summary>
        /// The level we should return to when "exit level" is selected in the
        /// pause menu, or when a level exit cannon is used.
        /// </summary>
        public LevelManifest LastHubWorld
            = ResourceLoader.Load<LevelManifest>("res://Levels/Production/TestMap/TestMap.level.tres");
        [JsonProperty("LastHubWorld")] private string _lastHubWorldFilePath
        {
            get => LastHubWorld?.ResourcePath;
            set => LastHubWorld = value == null
                ? null
                : ResourceLoader.Load<LevelManifest>(value);
        }

        /// <summary>
        /// The number of times the player has died outside of time trial mode.
        /// You don't get punished for this; it's just a fun little counter.
        /// </summary>
        [JsonProperty] public int TotalDeaths;

        /// <summary>
        /// The total amount of time the player has spent in a level or home
        /// world.
        ///
        /// Does NOT increase during loading screens (including the "mission
        /// results" screen)
        ///
        /// Does NOT increase during levels that don't have a
        /// <see cref="DiveBombLevel"/> as their root. (Though such levels
        /// shouldn't exist in the first place)
        ///
        /// Does NOT increase while the game is paused (including cutscenes that
        /// technically pause the game, such as the fairy kiss or fade-to-black)
        ///
        /// DOES increase while non-game-pausing cutscenes are playing (such as
        /// the vent animation)
        /// </summary>
        [JsonProperty] public PhysicsTicks TotalPlaytime;

        [JsonProperty] public Dictionary<string, LevelSaveData> Levels = new();

        public IEnumerable<string> VisitedLevels => Levels
            .Where(kvp => kvp.Value.VisitedOnce)
            .Select(kvp => kvp.Key);

        public int TotalGemsSpent => Levels.Values.Sum(l => l.Progress.SpentGems);
        public int TotalGemCount => Levels.Values.Sum(l => l.Progress.TotalGemsCollected) - TotalGemsSpent;
        public int TotalFairyCount => Levels.Values.Sum(l => l.Progress.CollectedFairies.Count);

        /// <summary>
        /// Data about your current visit to the level you're currently on.
        ///
        /// This is for data that should reset when the player leaves the level,
        /// but that also needs to persist if the player saves and loads
        /// mid-level.
        ///
        /// EG: The stats that we show after you reach the exit
        /// </summary>
        [JsonProperty] public LevelVisit CurrentLevelVisit = new();
        [JsonObject(MemberSerialization.OptIn)]
        public class LevelVisit
        {
            [JsonProperty] public string LastCheckpoint = null;

            /// <summary>
            /// Story flags that need to be persisted if the player saves/reloads
            /// mid-level, but that should still reset on revists.
            /// </summary>
            [JsonProperty] public HashSet<string> StoryFlags = new();

            [JsonProperty] public PhysicsTicks Playtime;
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

        public LevelSaveData GetLevelSaveData(LevelManifest level)
        {
            if (!Levels.ContainsKey(level.ResourcePath))
                Levels.Add(level.ResourcePath, new LevelSaveData());

            return Levels[level.ResourcePath];
        }

        public bool LevelExitReached(LevelManifest level) => GetLevelSaveData(level)
            .Progress
            .ExitReached;

        public double GetPercentComplete(LevelManifest level)
        {
            var levelSummary = AtlasCache.Instance.GetEntry(level);
            var progress = GetLevelSaveData(level).Progress;
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