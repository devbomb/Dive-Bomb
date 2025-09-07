using System;
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

            foreach (var category in Enum.GetValues<TimeTrialCategory>())
                AddCategoryRow(category);
        }

        private void AddCategoryRow(TimeTrialCategory category)
        {
            GD.Print("Adding row for " + category);

            var ttm = this.GetLevel().TimeTrial;

            string yourTime = ttm.RequirementsMet(category)
                ? TimeUtils.FormatPhysicsTicksStopwatch(ttm.TimerPhysicsTicks)
                : "did not qualify";

            uint? bestTimeTicks = ttm.GetSavedBestTime(category);
            string bestTime = bestTimeTicks != null
                ? TimeUtils.FormatPhysicsTicksStopwatch(bestTimeTicks.Value)
                : "--";

            var item = this.CreateItem();
            item.SetText(0, category.HumanReadableName());
            item.SetText(1, yourTime);
            item.SetText(2, bestTime);
        }
    }
}