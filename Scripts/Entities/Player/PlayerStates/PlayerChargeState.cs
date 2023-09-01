using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerChargeState : PlayerState
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

            if (!InputService.ChargeHeld)
                _player.ChangeState<PlayerWalkState>();
        }

        private void TurningControls(float delta)
        {
            // Rotate with the left stick
            float rotDeg = _player.RotationDegrees.Y;
            rotDeg -= InputService.LeftStick.X * Player.Charge.TurnSpeedDeg * delta;
            _player.RotationDegrees = new Vector3(0, rotDeg, 0);

            // Update the horizontal velocity, without changing the vertical
            // speed.
            Vector3 newVel = _player.GlobalForward() * Player.Charge.Speed;
            newVel.Y = _player.Velocity.Y;
            _player.Velocity = newVel;
        }

        private void ApplyGravity(float delta)
        {
            _player.Velocity += Vector3.Down * Player.Default.Gravity * delta;
        }

        private void UpdateCamera(float delta)
        {
            var camera = _player.Camera;

            camera.OrbitDistance = MathUtils.DecayToward(
                camera.OrbitDistance,
                Player.Charge.CameraDistance,
                Player.Charge.CameraDecayRate,
                delta
            );

            camera.OrbitPitchRad = AngleMath.DecayToward(
                camera.OrbitPitchRad,
                Mathf.DegToRad(Player.Charge.CameraPitchDeg),
                Player.Charge.CameraDecayRate,
                delta
            );

            camera.OrbitYawRad = AngleMath.DecayToward(
                camera.OrbitYawRad,
                _player.GlobalRotation.Y,
                Player.Charge.CameraDecayRate,
                delta
            );
        }
    }
}

