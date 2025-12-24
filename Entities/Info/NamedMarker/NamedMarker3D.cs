using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class NamedMarker3D : Node3D
    {
        [Export] public string MarkerId;
    }

    public static class NamedMarker3DNodeExtensions
    {
        public static NamedMarker3D GetNamedMarker3D(this Node node, string markerId)
        {
            return node.GetTree()
                .Root
                .EnumerateDescendantsOfType<NamedMarker3D>()
                .FirstOrDefault(m => m.MarkerId == markerId);
        }
    }
}