using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        [Export] public float WalkSpeed = 5f;
        [Export] public float Accel = 20;

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            AccelerateWithLeftStick(delta);
            UpdateRotationToMatchVelocity();

            ApplyGravity(delta);

            _player.MoveAndSlide();

            // Charge when the button is held
            if (InputService.ChargeHeld)
                _player.ChangeState<PlayerChargeState>();
        }

        private void AccelerateWithLeftStick(float delta)
        {
            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);

            Vector3 cameraRot = _player.Camera.Rotation;
            Vector3 leftStick3D =
                (Vector3.Right * leftStick2D.X) +
                (Vector3.Forward * leftStick2D.Y);
            leftStick3D = leftStick3D.Rotated(Vector3.Up, cameraRot.Y);

            // Update the velocity without affecting the vertical speed.
            Vector3 vel = _player.Velocity.Flattened();
            vel = _player.Velocity.MoveToward(
                leftStick3D * WalkSpeed,
                Accel * delta
            );
            vel.Y = _player.Velocity.Y;

            _player.Velocity = vel;
        }

        private void UpdateRotationToMatchVelocity()
        {
            if (!_player.Velocity.Flattened().IsZeroApprox())
            {
               float yAngleRad = Transform3D.Identity
                    .LookingAt(_player.Velocity.Flattened(), Vector3.Up)
                    .Basis
                    .GetEuler()
                    .Y;

                var rot = _player.GlobalRotation;
                rot.Y = yAngleRad;
                _player.GlobalRotation = rot;
            }
        }

        private void ApplyGravity(float delta)
        {
            _player.Velocity += Vector3.Down * _player.DefaultGravity * delta;
        }
    }
}

