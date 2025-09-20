using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class ConveyorBelt : StaticBody3D
    {
        [Export] public float Speed = 10;
        [Export] public string DirectionMarkerId;

        private Vector3 _normal;

        public override void _Ready()
        {
            // Find the normal of the "top" that the player would stand on.
            // The metadata we're searching will have been added by func_godot
            // when the map was imported.
            _normal = GetMeta("func_godot_mesh_data")
                .AsGodotDictionary()["normals"]
                .AsVector3Array()
                .OrderBy(n => n.Y)
                .Last();

            Callable.From(SetDirection).CallDeferred();
        }

        private void SetDirection()
        {
            var marker = GetTree()
                .Root
                .EnumerateDescendantsOfType<NamedMarker3D>()
                .FirstOrDefault(m => m.MarkerId == DirectionMarkerId);

            if (marker == null)
                throw new Exception($"Could not find conveyor belt direction marker with ID \"{DirectionMarkerId}\"");

            var direction = marker
                .GlobalForward()
                .ProjectOnPlane(_normal)
                .Normalized();

            ConstantLinearVelocity = direction * Speed;

            var scrollSpeed = new Vector2(direction.X, direction.Z).Normalized() * Speed;
            foreach (var meshInstance in this.EnumerateDescendantsOfType<MeshInstance3D>())
            {
                meshInstance.SetInstanceShaderParameter("scroll_speed", scrollSpeed);
            }
        }
    }
}