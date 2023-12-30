using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerGlideState : PlayerState
    {
        private const float ChargeDebounceDuration = 0.1f;
        private float _chargeDebounceTimer;

        public override void OnStateEntered()
        {
            _player.HasUsedGlide = true;
            _player.Camera.DisableInput = true;
            _player.Animator.Play("Glide", 0.3);

            _player.VSpeed = 0;

            _player.FSpeed = Mathf.Max(_player.FSpeed, Player.Glide.InitialFSpeed);
            _player.FSpeed = Mathf.Min(_player.FSpeed, Player.Glide.MaxFSpeed);

            _chargeDebounceTimer = ChargeDebounceDuration;
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

            _chargeDebounceTimer -= delta;

            TurningControls(
                _player.FSpeed,
                Player.Glide.TurnSpeedDeg,
                delta
            );

            _player.VSpeed = Mathf.MoveToward(
                _player.VSpeed,
                Player.Glide.TerminalVSpeed,
                Player.Glide.Gravity * delta
            );

            _player.FSpeed = Mathf.MoveToward(
                _player.FSpeed,
                Player.Glide.MaxFSpeed,
                Player.Glide.Accel * delta
            );

            _player.MoveAndSlide();

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            if (InputService.ChargeHeld && _chargeDebounceTimer <= 0)
            {
                _player.ChangeState<PlayerChargeFallState>();
                return;
            }
        }
    }
}

