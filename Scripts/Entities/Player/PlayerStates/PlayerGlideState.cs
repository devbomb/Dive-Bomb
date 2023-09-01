using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerGlideState : PlayerState
    {
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
            rotDeg -= InputService.LeftStick.X * Player.Glide.TurnSpeedDeg * delta;
            _player.RotationDegrees = new Vector3(0, rotDeg, 0);

            // Update the horizontal velocity, without changing the vertical
            // speed.
            Vector3 newVel = _player.GlobalForward() * Player.Glide.Speed;
            newVel.Y = _player.Velocity.Y;
            _player.Velocity = newVel;
        }

        private void ApplyGravity(float delta)
        {
            _player.Velocity += Vector3.Down * Player.Glide.Gravity * delta;
        }

        private void UpdateCamera(float delta)
        {
            var camera = _player.Camera;

            camera.OrbitDistance = MathUtils.DecayToward(
                camera.OrbitDistance,
                Player.Glide.CameraDistance,
                Player.Glide.CameraDecayRate,
                delta
            );

            camera.OrbitPitchRad = AngleMath.DecayToward(
                camera.OrbitPitchRad,
                Mathf.DegToRad(Player.Glide.CameraPitchDeg),
                Player.Glide.CameraDecayRate,
                delta
            );

            camera.OrbitYawRad = AngleMath.DecayToward(
                camera.OrbitYawRad,
                _player.GlobalRotation.Y,
                Player.Glide.CameraDecayRate,
                delta
            );
        }
    }
}

