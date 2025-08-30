using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class DiveBombLevel : Node3D
    {
        public bool IsTimeTrialMode => GetTree()
            .FindNode<TimeTrialManager>()
            ?.IsTimeTrialMode ?? false;

        public bool IsGemInInventory(Gem gem)
        {
            return GetProgress().IsGemCollected(gem.Value, gem.GetSaveKey());
        }

        public void AddGemToInventory(Gem gem)
        {
            GetProgress().CollectGem(gem.Value, gem.GetSaveKey());

            if (!IsTimeTrialMode)
            {
                var saveFile = SaveFile.Current;

                saveFile.AddUntalliedGem(gem.Value);
                GD.Print($"{saveFile.TotalGemCount}: Collected gem {gem.GetSaveKey()}");
            }
        }

        private SaveFile.LevelProgress GetProgress()
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