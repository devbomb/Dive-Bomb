using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerChargeState : PlayerState
    {
        [Export] public float ChargeSpeed = 5f;
        [Export] public float TurnSpeedDeg = 90;

        [Export] public float CameraZoomSpeed = 4;
        [Export] public float CameraRotSpeedDeg = 180;
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

            UpdateCamera(delta);

            if (!InputService.ChargeHeld)
                _player.ChangeState<PlayerWalkState>();
        }

        private void UpdateCamera(float delta)
        {
            var camera = _player.Camera;

            camera.OrbitDistance = Mathf.MoveToward(
                camera.OrbitDistance,
                CameraDistance,
                CameraZoomSpeed * delta
            );

            camera.OrbitPitchRad = AngleMath.MoveToward(
                camera.OrbitPitchRad,
                Mathf.DegToRad(CameraPitchDeg),
                Mathf.DegToRad(CameraRotSpeedDeg) * delta
            );

            camera.OrbitYawRad = AngleMath.MoveToward(
                camera.OrbitYawRad,
                _player.GlobalRotation.Y,
                Mathf.DegToRad(CameraRotSpeedDeg) * delta
            );
        }
    }
}

