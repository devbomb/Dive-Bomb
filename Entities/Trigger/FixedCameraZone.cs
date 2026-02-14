using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class FixedCameraZone : Area3D
    {
        [Export] public string target;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player player)
            {
                var marker = this.FindNodeByTargetName<NamedMarker3D>(target);
                player.Camera.FixPosition(marker.GlobalTransform);
            }
        }

        public void OnBodyExited(Node3D body)
        {
            if (body is Player player)
            {
                player.Camera.StartFollowing(1);
            }
        }
    }
}