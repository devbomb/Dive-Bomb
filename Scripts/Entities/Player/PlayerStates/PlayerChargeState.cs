using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerChargeState : PlayerState
    {
        [Export] public float ChargeSpeed = 10f;
        [Export] public float TurnSpeedDeg = 90;

        [Export] public float CameraDecayRate = 10;
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

            camera.OrbitDistance = MathUtils.DecayToward(
                camera.OrbitDistance,
                CameraDistance,
                CameraDecayRate,
                delta
            );

            camera.OrbitPitchRad = AngleMath.DecayToward(
                camera.OrbitPitchRad,
                Mathf.DegToRad(CameraPitchDeg),
                CameraDecayRate,
                delta
            );

            camera.OrbitYawRad = AngleMath.DecayToward(
                camera.OrbitYawRad,
                _player.GlobalRotation.Y,
                CameraDecayRate,
                delta
            );
        }
    }
}

