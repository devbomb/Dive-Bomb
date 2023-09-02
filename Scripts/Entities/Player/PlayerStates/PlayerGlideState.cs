using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerGlideState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraLockedState>();
            SetVSpeed(0);
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

            ContinuouslyRecenterCamera(
                Player.Glide.CameraDistance,
                Player.Glide.CameraPitchDeg,
                Player.Glide.CameraDecayRate,
                delta
            );

            if (_player.IsOnFloor())
                _player.ChangeState<PlayerWalkState>();
        }
    }
}

