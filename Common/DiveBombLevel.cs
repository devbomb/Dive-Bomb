using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class DiveBombLevel : Node3D
    {
        /// <summary>
        /// The current level's human-friendly name.
        /// Will be stored in the Atlas cache.
        /// </summary>
        /// <returns></returns>
        [Export] public string LevelName;

        /// <summary>
        /// The level we should return to when "exit level" is selected in the
        /// pause menu, or when a vortex is used.
        ///
        /// Set to null to indicate that this level is a home world.
        /// </summary>
        /// <returns></returns>
        [Export(PropertyHint.File)] public string HomeWorldLevel;

        /// <summary>
        /// Set this to true to prevent the player from exiting the level
        /// through the pause menu, even if this level isn't a home world.
        ///
        /// This is primarily used for the intro tutorial, to prevent the player
        /// from skipping it.
        ///
        /// THIS VALUE WILL BE IGNORED if the player has already reached the
        /// level exit canon at least once.
        /// </summary>
        [Export] public bool ForbidExitLevel;

        public bool IsHubWorld => HomeWorldLevel == null;

        public static DiveBombLevel GetLevel(Node node) => node.GetLevel();

        public readonly TimeTrialManager TimeTrial = new TimeTrialManager();

        public int TotalGems => TimeTrial.IsTimeTrialMode
            ? GetProgress().TotalGemsCollected - GetProgress().SpentGems
            : SaveFileManager.Current.TotalGemCount;

        /// <summary>
        /// Story flags that should persist even between level visits.
        /// If you want something that will reset when you re-visit the level,
        /// use <see cref="TempStoryFlags"/> instead.
        ///
        /// Shorthand for GetProgress().StoryFlags.
        /// </summary>
        public HashSet<string> PermanentStoryFlags => GetProgress().StoryFlags;

        /// <summary>
        /// Story flags that need to be persisted if the player saves/reloads
        /// mid-level, but that should still reset on revists.
        ///
        /// Shorthand for SaveFileManager.Current.CurrentLevelVisit.StoryFlags.
        /// </summary>
        public HashSet<string> TempStoryFlags => SaveFileManager
            .Current
            .CurrentLevelVisit
            .StoryFlags;

        public DiveBombLevel()
        {
            AddChild(TimeTrial);
        }

        public override void _Ready()
        {
            AtlasCache.Instance.UpdateCache(SceneFilePath, this);

            // Start a new level visit
            // ...unless the game is currently being loaded from a save file,
            // in which case we don't want to overwrite the existing level visit.
            bool isLoadingSaveFile = SaveFileManager.Current.CurrentLevel == SceneFilePath;
            if (!isLoadingSaveFile)
            {
                SaveFileManager.Current.CurrentLevel = SceneFilePath;
                SaveFileManager.Current.CurrentLevelVisit = new();
                SaveFileManager.Instance.RequestAutosave();
            }
        }

        public override void _PhysicsProcess(double delta)
        {
            // Why increment the playtime _here_, instead of some autoload
            // singleton?  Simple: we don't want it to increase during a loading
            // screen.  Doing it here ensures that for free.
            SaveFileManager.Current.TotalPlaytime++;
            SaveFileManager.Current.CurrentLevelVisit.Playtime++;
        }

        public bool CanExitLevel()
        {
            if (IsHubWorld)
                return false;

            if (ForbidExitLevel && !GetProgress().ExitReached)
                return false;

            return true;
        }

        public LevelProgress GetProgress()
        {
            return TimeTrial.IsTimeTrialMode
                ? TimeTrial.DummyProgress
                : SaveFileManager.Current.GetLevelSaveData(SceneFilePath).Progress;
        }

        public LevelSummary GetSummary()
        {
            if (!IsNodeReady())
                throw new System.Exception("Don't call GetSummary() before the level is ready!");

            return AtlasCache.Instance.GetEntry(SceneFilePath);
        }
    }

    public static class DiveBombLevelExtensions
    {
        /// <summary>
        /// Returns the DiveBombLevel that this node is a descendant of, or null
        /// if it is not inside a level.
        ///
        /// If the node is itself a DiveBombLevel, then it is considered its own
        /// level.
        /// </summary>
        public static DiveBombLevel GetLevel(this Node node)
        {
            if (node is DiveBombLevel l)
                return l;

            return node.GetParent()?.GetLevel();
        }

        public static bool IsTimeTrialMode(this Node node)
        {
            return node.GetLevel()?.TimeTrial?.IsTimeTrialMode ?? false;
        }
    }
}