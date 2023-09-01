using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerGlideState : PlayerState
    {
        [Export] public float GlideSpeed = 9f;
        [Export] public float TurnSpeedDeg = 90;
        [Export] public float GlideGravity = 2;

        [Export] public float CameraDecayRate = 10;
        [Export] public float CameraPitchDeg = 0;
        [Export] public float CameraDistance = 6;

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraLockedState>();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            TurningControls(delta);
            ApplyGravity(delta);

            _player.MoveAndSlide();

            UpdateCamera(delta);

            if (_player.IsOnFloor())
                _player.ChangeState<PlayerWalkState>();
        }

        private void TurningControls(float delta)
        {
            // Rotate with the left stick
            float rotDeg = _player.RotationDegrees.Y;
            rotDeg -= InputService.LeftStick.X * TurnSpeedDeg * delta;
            _player.RotationDegrees = new Vector3(0, rotDeg, 0);

            // Update the horizontal velocity, without changing the vertical
            // speed.
            Vector3 newVel = _player.GlobalForward() * GlideSpeed;
            newVel.Y = _player.Velocity.Y;
            _player.Velocity = newVel;
        }

        private void ApplyGravity(float delta)
        {
            _player.Velocity += Vector3.Down * GlideGravity * delta;
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

