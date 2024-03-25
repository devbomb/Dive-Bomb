using Godot;

namespace FastDragon
{
    public partial class UsesRenderLayersDecorator : Node
    {
        [Export(PropertyHint.Layers3DRender)] public uint Layers = 1;

        public override void _Ready()
        {
            foreach (var child in GetParent().EnumerateDescendantsOfType<VisualInstance3D>())
            {
                child.Layers = Layers;
            }
        }
    }
}