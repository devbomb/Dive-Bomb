using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialCategoryHighScoresTable : Tree
    {
        public void Refresh()
        {
            this.SetColumnTitle(0, "Category");
            this.SetColumnTitle(1, "Your time");
            this.SetColumnTitle(2, "Best time");

            Clear();
            this.CreateItem();

            var level = this.GetLevel();
            if (level != null)
            {
                var categories = Enum
                    .GetValues<TimeTrialCategory>()
                    .Where(level.TimeTrial.IsRelevant);

                foreach (var category in categories)
                    AddCategoryRow(category);
            }
        }

        private void AddCategoryRow(TimeTrialCategory category)
        {
            GD.Print("Adding row for " + category);

            var ttm = this.GetLevel().TimeTrial;

            string yourTime = ttm.RequirementsMet(category)
                ? ttm.TimerPhysicsTicks.FormatStopwatch()
                : "did not qualify";

            PhysicsTicks? bestTimeTicks = ttm.GetSavedBestTime(category);
            string bestTime = bestTimeTicks != null
                ? bestTimeTicks.Value.FormatStopwatch()
                : "--";

            var item = this.CreateItem();
            item.SetText(0, category.HumanReadableName());
            item.SetText(1, yourTime);
            item.SetText(2, bestTime);
        }
    }
}