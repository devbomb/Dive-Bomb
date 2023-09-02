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

            MoveAndSlideHandlingChargables(delta);

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

        private void MoveAndSlideHandlingChargables(float delta)
        {
            Vector3 motion = _player.Velocity * delta;

            while (true)
            {
                KinematicCollision3D collision = _player.MoveAndCollide(motion);

                if (collision == null)
                    return;

                motion = collision.GetRemainder();

                // Keep plowing through if it was a chargeable
                if (collision.GetCollider() is IChargeable c)
                {
                    // TODO: Bonk if bonking is enabled
                    c.OnCharged();
                    continue;
                }

                // Project the motion onto the surface to cause a slide
                motion = motion.ProjectOnPlane(collision.GetNormal());
            }
        }
    }
}

