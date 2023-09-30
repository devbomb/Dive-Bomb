using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerChargeState : PlayerState
    {
        public override bool AllowFlaming => false;

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraLockedState>();
            _player.Animator.Play("Charge");
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithGroundSlope(delta);

            ContinuouslyRecenterCamera(
                Player.Charge.CameraDistance,
                Player.Charge.CameraPitchDeg,
                Player.Charge.CameraDecayRate,
                delta
            );
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            // TODO: Add a little bit of acceleration when on the ground, but
            // _instant_ acceleration when doing a charge-jump, to mimic Spyro 1
            TurningControls(
                Player.Charge.GroundSpeed,
                Player.Charge.GroundTurnSpeedDeg,
                delta
            );
            ApplyGravity(delta);

            MoveAndSlideStepByStep(delta, OnChargedIntoSomething);

            if (IsTouchingWallAtBonkAngle())
            {
                _player.ChangeState<PlayerBonkState>();
                return;
            }

            if (!InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerChargeFallState>();
                return;
            }

            // The player is allowed to "gallop" by holding charge and jump,
            // so check if jump is held here instead of checking if it's just
            // pressed.
            if (InputService.JumpHeld)
            {
                _player.ChangeState<PlayerChargeJumpState>();
                return;
            }
        }
    }
}

