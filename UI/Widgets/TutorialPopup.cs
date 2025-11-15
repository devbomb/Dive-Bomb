using System.Linq;
using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class TutorialPopup : Control
    {
        [Export] public string TriggerName;
        [Export] public double FadeInDuration = 0.25;
        [Export] public double FadeOutDuration = 0.5;

        private bool _playerInside;
        private NamedTriggerZone _trigger;

        public override void _Ready()
        {
            // It's often convenient to make these invisible in the editor
            // (so we can edit one popup without getting distracted by another),
            // but we still need it to be visible in-game.
            Visible = true;

            Callable.From(() =>
            {
                _trigger = GetTree()
                    .Root
                    .EnumerateDescendantsOfType<NamedTriggerZone>()
                    .First(t => t.TriggerName == TriggerName);
            }).CallDeferred();
        }

        public override void _PhysicsProcess(double delta)
        {
            _playerInside = _trigger.GetOverlappingBodiesResetSafe()
                .OfType<Player>()
                .Any();
        }

        public override void _Process(double delta)
        {
            float targetAlpha = _playerInside
                ? 1f
                : 0f;

            float speed = _playerInside
                ? 1f / (float)FadeInDuration
                : 1f / (float)FadeOutDuration;

            var color = Modulate;
            color.A = Mathf.MoveToward(color.A, targetAlpha, speed * (float)delta);
            Modulate = color;
        }
    }
}