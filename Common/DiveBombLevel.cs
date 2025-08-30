using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class DiveBombLevel : Node3D
    {
        public bool IsTimeTrialMode => GetTree()
            .FindNode<TimeTrialManager>()
            ?.IsTimeTrialMode ?? false;

        public int TotalGems => IsTimeTrialMode
            ? GetProgress().TotalGemsCollected - GetProgress().SpentGems
            : SaveFile.Current.TotalGemCount;

        public bool IsGemInInventory(Gem gem)
        {
            return GetProgress().IsGemCollected(gem.Value, gem.SaveKey);
        }

        public void AddGemToInventory(Gem gem)
        {
            GetProgress().CollectGem(gem.Value, gem.SaveKey);

            if (!IsTimeTrialMode)
            {
                var saveFile = SaveFile.Current;

                saveFile.AddUntalliedGem(gem.Value);
                GD.Print($"{saveFile.TotalGemCount}: Collected gem {gem.SaveKey}");
            }
        }

        public bool IsFairyInInventory(Fairy fairy)
        {
            return GetProgress().CollectedFairies.Contains(fairy.SaveKey);
        }

        public void AddFairyToInventory(Fairy fairy)
        {
            GetProgress().CollectedFairies.Add(fairy.SaveKey);
        }

        public void SpendGems(int amount)
        {
            GetProgress().SpentGems += amount;

            if (!IsTimeTrialMode)
            {
                SaveFile.Current.AddUntalliedSpentGems(amount);
            }
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