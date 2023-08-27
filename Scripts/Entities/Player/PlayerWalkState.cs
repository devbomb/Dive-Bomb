using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        [Export] public float WalkSpeed = 2.5f;
        [Export] public float Accel = 20;

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            var leftStick2D = InputService.LeftStick;
            leftStick2D = leftStick2D.LimitLength(1);

            Vector3 cameraRot = GetViewport().GetCamera3D().Rotation;
            Vector3 leftStick3D =
                (Vector3.Right * leftStick2D.X) +
                (Vector3.Forward * leftStick2D.Y);
            leftStick3D = leftStick3D.Rotated(Vector3.Up, cameraRot.Y);

            Vector3 vel = _player.Velocity.Flattened();
            vel = _player.Velocity.MoveToward(
                leftStick3D * WalkSpeed,
                Accel * delta
            );
            vel.Y = _player.Velocity.Y;

            _player.Velocity = vel;
            _player.MoveAndSlide();

            if (!_player.Velocity.Flattened().IsZeroApprox())
            {
                _player.Rotation = Transform3D.Identity
                    .LookingAt(_player.Velocity.Flattened(), Vector3.Up)
                    .Basis
                    .GetEuler();
            }
        }
    }
}

