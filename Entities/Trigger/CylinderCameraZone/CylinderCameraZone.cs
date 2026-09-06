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
        private class SuggestingCylinder(CylinderCameraZone Owner) : PlayerCamera.CustomState
        {
            public override void OnStateEntered()
            {
                CustomPosition = Owner.CalculateCameraPos();
                base.OnStateEntered();
            }

            public override void _PhysicsProcess(double delta)
            {
                CustomPosition = Owner.CalculateCameraPos();
                base._PhysicsProcess(delta);
            }

            public override void OnOrbitRequested(float yawRad, float pitchRad)
            {
                Self.DetectAnglesAndDistance(); // Mouselook breaks without this, for some reason.
                Self.StartFollowing(0.1f);
            }
        }

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
            if (body is Player player)
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
                player.Camera.DetectAnglesAndDistance(); // Mouselook breaks without this, for some reason.
                player.Camera.StartFollowing(1);
            }
        }

        private Transform3D CalculateCameraPos()
        {
            var cameraForward = _player
                .CameraFocus
                .GlobalPosition
                .DirectionTo(_targetPos)
                .Flattened()
                .Normalized();

            var cameraPos = _targetPos - (cameraForward * Distance);
            cameraPos.Y = _player.CameraFocus.GlobalPosition.Y;

            return Transform3D.Identity
                .Translated(cameraPos)
                .LookingAt(_player.CameraFocus.GlobalPosition);
        }
    }
}