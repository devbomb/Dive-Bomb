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

            TurningControls(
                Player.Charge.Speed,
                Player.Charge.TurnSpeedDeg,
                delta
            );
            ApplyGravity(delta);

            _player.MoveAndSlide();

            ContinuouslyRecenterCamera(
                Player.Charge.CameraDistance,
                Player.Charge.CameraPitchDeg,
                Player.Charge.CameraDecayRate,
                delta
            );

            if (!InputService.ChargeHeld)
                _player.ChangeState<PlayerWalkState>();
        }
    }
}

