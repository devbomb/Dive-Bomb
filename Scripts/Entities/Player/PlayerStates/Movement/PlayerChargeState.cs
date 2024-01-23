using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerChargeState : PlayerState
    {
        public override bool AllowFlaming => false;
        public override bool DisableCameraInput => true;
        public override bool SpawningGemsHomeIn => true;

        private const float MinSkitterDelay = 1f / 30;
        private const float RollingRadius = 0.5f;
        private const float RollingCircumference = 2 * Mathf.Pi * RollingRadius;

        private float _fspeed;
        private bool _disableJump;

        public override void OnStateEntered()
        {
            _player.Animator.Play("Charge");

            _fspeed = _player.Velocity.Flattened().Length();
            _fspeed = Mathf.Max(_fspeed, Player.Charge.InitialGroundSpeed);

            _player.Animator.SpeedScale = _player.Velocity.Length() / RollingCircumference;
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
            _player.Animator.SpeedScale = 1;
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

            _fspeed = Mathf.MoveToward(
                _fspeed,
                Player.Charge.MaxGroundSpeed,
                Player.Charge.GroundAccel * delta
            );

            TurningControls(
                _fspeed,
                Player.Charge.TurnSpeedDeg,
                delta
            );
            ApplyGravity(delta);

            if (MoveAndSlideCharging(delta))
                return;

            if (!InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerWalkAfterChargingState>();
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
            if (InputService.JumpHeld && !_disableJump)
            {
                _player.ChangeState<PlayerChargeJumpState>();

                // Impose a cooldown on charge-jumping again, so the player
                // can't skitter faster than they would in Spyro.
                // This cooldown needs to persist in-between states, to allow
                // instant galloping in non-skitter situations.
                _disableJump = true;

                var timer = GetTree().CreateTimer(
                    timeSec: MinSkitterDelay,
                    processAlways: false,
                    processInPhysics: true
                );
                timer.Timeout += () => _disableJump = false;

                return;
            }
        }
    }
}

