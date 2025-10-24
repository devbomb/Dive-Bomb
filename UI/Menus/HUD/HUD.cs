using Godot;

namespace FastDragon
{
    public partial class HUD : Control
    {
        [Export] public CountingLabel GemCountLabel;
        [Export] public ContextuallyHiddenControl GemCountHider;

        public override void _Process(double delta)
        {
            GemCountHider.ObservedValue = GemCount();
            GemCountLabel.Value = GemCount();
        }

        private int GemCount() => this.GetLevel()?.TotalGems ?? 0;
    }
}