using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class ConveyorBelt : StaticBody3D
    {
        [Export] public float Speed = 10;

        public override void _Ready()
        {
            ConstantLinearVelocity = Vector3.Forward * Speed;

            foreach (var meshInstance in this.EnumerateDescendantsOfType<MeshInstance3D>())
            {
                meshInstance.SetInstanceShaderParameter("scroll_speed", GetScrollSpeed());
            }
        }

        private Vector2 GetScrollSpeed()
        {
            return new Vector2(0, -Speed);
        }
    }
}