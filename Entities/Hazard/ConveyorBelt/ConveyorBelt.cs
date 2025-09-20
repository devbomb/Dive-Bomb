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

        public override void _Ready()
        {
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

            var direction = marker.GlobalForward();
            ConstantLinearVelocity = direction * Speed;

            foreach (var meshInstance in this.EnumerateDescendantsOfType<MeshInstance3D>())
            {
                meshInstance.SetInstanceShaderParameter("scroll_speed", GetScrollSpeed());
            }
        }

        private Vector2 GetScrollSpeed()
        {
            return new Vector2(0, Speed);
        }
    }
}