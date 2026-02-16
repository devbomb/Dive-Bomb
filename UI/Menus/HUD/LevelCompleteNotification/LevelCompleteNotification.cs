using Godot;
using System;

namespace FastDragon
{
    public partial class LevelCompleteNotification : Control
    {
        [ExportCategory("Internal")]
        [Export] public AnimationPlayer Animator;

        public override void _Ready()
        {
            SignalBus.Instance.ItemCollected += OnItemCollected;
        }

        private void OnItemCollected()
        {
            var level = this.GetLevel();
            if (level == null)
                return;

            var progress = level.GetProgress();
            int fairies = progress.CollectedFairies.Count;
            int gems = progress.TotalGemsCollected;

            var summary = level.GetSummary();
            int totalFairies = summary.TotalFairiesInLevel;
            int totalGems = summary.TotalGemsInLevel;

            if (fairies >= totalFairies && gems >= totalGems)
                Animator.Play("Showing");
        }
    }
}
