using System.Linq;
using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class TutorialPopup : Control, IPowerable
    {
        [Export] public string targetname { get; set; }
        [Export] public double FadeInDuration = 0.25;
        [Export] public double FadeOutDuration = 0.5;

        private bool _showing;

        public override void _Ready()
        {
            // It's often convenient to make these invisible in the editor
            // (so we can edit one popup without getting distracted by another),
            // but we still need it to be visible in-game.
            Visible = true;
        }

        public void SetPowered(bool powered)
        {
            _showing = powered;
        }

        public void ForceSetPowered(bool powered)
        {
            _showing = powered;

            var color = Modulate;
            color.A = _showing
                ? 1
                : 0;

            Modulate = color;
        }

        public override void _Process(double delta)
        {
            float targetAlpha = _showing
                ? 1f
                : 0f;

            float speed = _showing
                ? 1f / (float)FadeInDuration
                : 1f / (float)FadeOutDuration;

            var color = Modulate;
            color.A = Mathf.MoveToward(color.A, targetAlpha, speed * (float)delta);
            Modulate = color;
        }
    }
}