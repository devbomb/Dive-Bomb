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

            // TODO: Add a little bit of acceleration when on the ground, but
            // _instant_ acceleration when doing a charge-jump, to mimic Spyro 1
            TurningControls(
                Player.Charge.GroundSpeed,
                Player.Charge.GroundTurnSpeedDeg,
                delta
            );
            ApplyGravity(delta);

            MoveAndSlideStepByStep(delta, OnHitSomething);

            ContinuouslyRecenterCamera(
                Player.Charge.CameraDistance,
                Player.Charge.CameraPitchDeg,
                Player.Charge.CameraDecayRate,
                delta
            );

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

        private MoveAndSlideAction OnHitSomething(GodotObject hitObject)
        {
            if (hitObject is IChargeable c)
            {
                c.OnCharged();
                return c.CausesBonk
                    ? MoveAndSlideAction.Stop
                    : MoveAndSlideAction.ContinueThroughObject;
            }

            return MoveAndSlideAction.ContinueSliding;
        }
    }
}

