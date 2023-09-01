using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerChargeState : PlayerState
    {
        [Export] public float ChargeSpeed = 5f;
        [Export] public float TurnSpeedDeg = 90;

        [Export] public float CameraPitchDeg = 0;
        [Export] public float CameraDistance = 5;

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraLockedState>();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            float rotDeg = _player.RotationDegrees.Y;
            rotDeg -= InputService.LeftStick.X * TurnSpeedDeg * delta;
            _player.RotationDegrees = new Vector3(0, rotDeg, 0);

            _player.Velocity = _player.GlobalForward() * ChargeSpeed;
            _player.MoveAndSlide();

            _player.Camera.OrbitDistance = CameraDistance;
            _player.Camera.OrbitPitchRad = Mathf.DegToRad(CameraPitchDeg);
            _player.Camera.OrbitYawRad = _player.GlobalRotation.Y;

            if (!InputService.ChargeHeld)
                _player.ChangeState<PlayerWalkState>();
        }
    }
}

