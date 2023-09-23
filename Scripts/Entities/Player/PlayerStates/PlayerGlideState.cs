using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerGlideState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraLockedState>();
            _player.Animator.Play("Glide");
            _player.VSpeed = 0;
        }

        public override void _Process(double deltaD)
        {
            ContinuouslyRecenterCamera(
                Player.Charge.CameraDistance,
                Player.Charge.CameraPitchDeg,
                Player.Charge.CameraDecayRate,
                (float)deltaD
            );
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            TurningControls(
                Player.Glide.Speed,
                Player.Glide.TurnSpeedDeg,
                delta
            );
            ApplyGravity(delta, Player.Glide.Gravity);

            _player.MoveAndSlide();

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            if (InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerChargeFallState>();
                return;
            }
        }
    }
}

