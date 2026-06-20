using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TutorialPopupZone : Area3D
    {
        [Export] public string Text = "";

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
            _label.Text = Text;
        }

        public override void _PhysicsProcess(double delta)
        {
            _label.Visible = GetOverlappingBodies().Any();
        }
    }
}