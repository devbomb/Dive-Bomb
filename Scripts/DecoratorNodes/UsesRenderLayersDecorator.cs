using Godot;

namespace FastDragon
{
    [Tool]
    public partial class UsesRenderLayersDecorator : Node
    {
        [Export(PropertyHint.Layers3DRender)] public uint Layers
        {
            get => _layers;
            set
            {
                _layers = value;
                UpdateRenderLayers();
            }
        }

        private uint _layers = 1;

        public override void _EnterTree()
        {
            UpdateRenderLayers();
        }

        private void UpdateRenderLayers()
        {
            if (GetParent() == null)
                return;

            foreach (var node in GetParent().EnumerateDescendantsOfType<VisualInstance3D>())
            {
                // In the editor, don't edit nodes that are actually part of the
                // current scene file.  That's just annoying.
                if (Engine.IsEditorHint() && node.Owner == Owner)
                    continue;

                node.Layers = Layers;
            }
        }
    }
}