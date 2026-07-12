using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TutorialPopupZone : Area3D
    {
        [Export] public string Text = "";
        [Export] public double FadeInDuration = 0.25;
        [Export] public double FadeOutDuration = 0.5;

        private bool _showing;

        private readonly RichTextLabel _label = new()
        {
            Theme = ResourceLoader.Load<Theme>("res://Themes/Themes/TutorialPopup.tres"),

            BbcodeEnabled = true,
            FitContent = true,
            AutowrapMode = TextServer.AutowrapMode.Off,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,

            AnchorBottom = 1,
            OffsetBottom = -20,

            AnchorTop = 1,
            OffsetTop = 0,

            AnchorLeft = 0.5f,
            OffsetLeft = 0,

            AnchorRight = 0.5f,
            OffsetRight = 0,

            GrowVertical = Control.GrowDirection.Begin,
            GrowHorizontal = Control.GrowDirection.Both,
        };

        public TutorialPopupZone()
        {
            AddChild(_label);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            _label.Text = Text;
        }

        private void Reset()
        {
            _showing = false;

            var color = _label.Modulate;
            color.A = 0;
            _label.Modulate = color;
        }

        public override void _PhysicsProcess(double delta)
        {
            _showing = GetOverlappingBodies().Any();
        }

        public override void _Process(double delta)
        {
            float targetAlpha = _showing
                ? 1f
                : 0f;

            float speed = _showing
                ? 1f / (float)FadeInDuration
                : 1f / (float)FadeOutDuration;

            var color = _label.Modulate;
            color.A = Mathf.MoveToward(color.A, targetAlpha, speed * (float)delta);
            _label.Modulate = color;
        }
    }
}