using System.Linq;
using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class TutorialPopupZone : Area3D
    {
        [Export] public Control ControlToShow;
        [Export] public double FadeInDuration = 0.25;
        [Export] public double FadeOutDuration = 0.5;

        private bool _playerInside;

        public override void _Ready()
        {
            // It's often convenient to make these invisible in the editor
            // (so we can edit one popup without getting distracted by another),
            // but we still need it to be visible in-game.
            ControlToShow.Visible = true;
        }

        public override void _PhysicsProcess(double delta)
        {
            _playerInside = this.GetOverlappingBodiesResetSafe()
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

            var color = ControlToShow.Modulate;
            color.A = Mathf.MoveToward(color.A, targetAlpha, speed * (float)delta);
            ControlToShow.Modulate = color;
        }
    }
}