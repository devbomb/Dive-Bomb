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