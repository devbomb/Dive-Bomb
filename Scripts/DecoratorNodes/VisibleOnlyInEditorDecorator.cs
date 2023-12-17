using Godot;

namespace FastDragon
{
    public partial class VisibleOnlyInEditorDecorator : Node
    {
        public override void _Ready()
        {
            switch (GetParent())
            {
                case Node2D p: p.Visible = false; break;
                case Node3D p: p.Visible = false; break;
                case Control p: p.Visible = false; break;
                default: throw new System.Exception("Parent node does not have a Visible property");
            }
        }
    }
}