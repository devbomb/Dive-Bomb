using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class CylinderCameraZone : Area3D
    {
        [Export] public string TargetMarkerId;
        [Export] public float Distance = 20;

        private Player _player;
        private Vector3 _targetPos;

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;
            BodyExited += OnBodyExited;
            SignalBus.Instance.LevelReset += Reset;
        }

        public void Reset()
        {
            _player = null;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_player != null)
            {
                _player.Camera.ManhandledPosition = CalculateCameraPos();
            }
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is Player player)
            {
                _targetPos = GetTree().CurrentScene
                    .EnumerateDescendantsOfType<NamedMarker3D>()
                    .First(m => m.MarkerId == TargetMarkerId)
                    .GlobalPosition;

                _player = player;

                player.Camera.StartManhandling(CalculateCameraPos(), 1);
            }
        }

        public void OnBodyExited(Node3D body)
        {
            if (body is Player player)
            {
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