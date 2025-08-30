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

        public static DiveBombLevel GetLevel(Node node) => node.GetLevel();

        public bool IsTimeTrialMode => GetTree()
            .FindNode<TimeTrialManager>()
            ?.IsTimeTrialMode ?? false;

        public int TotalGems => IsTimeTrialMode
            ? GetProgress().TotalGemsCollected - GetProgress().SpentGems
            : SaveFile.Current.TotalGemCount;

        public override void _Ready()
        {
            AtlasCache.Instance.UpdateCache(SceneFilePath, this);
        }

        public SaveFile.LevelProgress GetProgress()
        {
            // TODO: Return a separate one if we're in time trial mode
            return SaveFile.Current.CurrentLevelProgress;
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
    }
}