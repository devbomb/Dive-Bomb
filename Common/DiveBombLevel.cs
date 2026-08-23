using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class DiveBombLevel : Node3D
    {
        [Export] public LevelManifest Manifest;

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
            AtlasCache.Instance.UpdateCache(this);

            if (Manifest.IsHubWorld)
                SaveFileManager.Current.LastHubWorld = Manifest;

            // Start a new level visit
            // ...unless the game is currently being loaded from a save file,
            // in which case we don't want to overwrite the existing level visit.
            bool isLoadingSaveFile = SaveFileManager.Current.CurrentLevel == Manifest;
            if (!isLoadingSaveFile)
            {
                SaveFileManager.Current.CurrentLevel = Manifest;
                SaveFileManager.Current.CurrentLevelVisit = new();
                SaveFileManager.Current.GetLevelSaveData(Manifest).VisitedOnce = true;
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
            if (Manifest.IsHubWorld)
                return false;

            if (ForbidExitLevel && !GetProgress().ExitReached)
                return false;

            return true;
        }

        public LevelProgress GetProgress()
        {
            return TimeTrial.IsTimeTrialMode
                ? TimeTrial.DummyProgress
                : SaveFileManager.Current.GetLevelSaveData(Manifest).Progress;
        }

        public LevelCollectableSummary GetCollectableSummary()
        {
            if (!IsNodeReady())
                throw new System.Exception("Don't call GetSummary() before the level is ready!");

            return AtlasCache.Instance.GetEntry(Manifest);
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