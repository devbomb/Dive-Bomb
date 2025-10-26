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

        public bool IsHomeWorld => HomeWorldLevel == null;

        public static DiveBombLevel GetLevel(Node node) => node.GetLevel();

        public readonly TimeTrialManager TimeTrial = new TimeTrialManager();

        public int TotalGems => TimeTrial.IsTimeTrialMode
            ? GetProgress().TotalGemsCollected - GetProgress().SpentGems
            : SaveFileManager.Current.TotalGemCount;

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