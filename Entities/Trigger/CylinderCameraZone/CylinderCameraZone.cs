using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class CylinderCameraZone : Area3D
    {
        [Export] public string target;
        [Export] public float Distance = 20;

        private Player _player;
        private Vector3 _targetPos;

        private readonly SuggestingCylinder _cameraState;

        public CylinderCameraZone()
        {
            _cameraState = new(this);
        }

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !player.Camera.IsUsingMouselook)
            {
                _targetPos = GetTree().CurrentScene
                    .EnumerateDescendantsOfType<NamedMarker3D>()
                    .First(m => m.targetname == target)
                    .GlobalPosition;

                _player = player;

                player.Camera.ChangeState(_cameraState);
            }
        }

        public void OnBodyExited(Node3D body)
        {
            if (body is Player player && player.Camera.CurrentState == _cameraState)
            {
                player.Camera.StartFollowing(1);
            }
        }

        private class SuggestingCylinder(CylinderCameraZone Owner) : PlayerCamera.CustomState
        {
            protected override Transform3D GetCustomPosition()
            {
                var cameraForward = Player
                    .CameraFocus
                    .GlobalPosition
                    .DirectionTo(Owner._targetPos)
                    .Flattened()
                    .Normalized();

                var cameraPos = Owner._targetPos - (cameraForward * Owner.Distance);
                cameraPos.Y = Player.CameraFocus.GlobalPosition.Y;

                return Transform3D.Identity
                    .Translated(cameraPos)
                    .LookingAt(Player.CameraFocus.GlobalPosition);
            }

            public override void OnOrbitRequested(float yawRad, float pitchRad)
            {
                Self.StartFollowing(0.1f);
            }
        }
    }
}