using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerRollState : PlayerState
    {
        public override bool AllowFlaming => false;
        public override bool DisableCameraInput => true;
        public override bool SpawningGemsHomeIn => true;

        private const float MinSkitterDelay = 1f / 30;
        private const float RollingRadius = 0.5f;
        private const float RollingCircumference = 2 * Mathf.Pi * RollingRadius;

        private float _timer;

        public override void OnStateEntered()
        {
            _player.Animator.Play("Roll");
            _player.Velocity = _player.GlobalForward() * Player.Roll.InitialSpeed;

            _timer = 0;
        }

        public override void OnStateExited()
        {
            _player.Animator.SpeedScale = 1;
        }

        public override void _Process(double deltaD)
        {
            _player.Animator.SpeedScale = _player.Velocity.Length() / RollingCircumference;

            ContinuouslyRecenterCamera(
                Player.Roll.CameraDistance,
                Player.Roll.CameraPitchDeg,
                Player.Roll.CameraDecayRate,
                (float)deltaD
            );
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                _player.ChangeState<PlayerWalkJumpState>();
                return;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyGravity(delta);

            LeftStickControls(delta);

            if (MoveAndSlideRolling(delta))
                return;

            _timer += delta;

            if (_timer >= Player.Roll.FrictionlessDuration)
            {
                // Apply friction
                _player.FSpeed = Mathf.MoveToward(
                    _player.FSpeed,
                    TargetSpeed(),
                    Player.Roll.Friction * delta
                );
            }

            if (_timer >= Player.Roll.MinDuration && !InputService.RollHeld)
            {
                if (_player.IsOnFloor())
                {
                    _player.ChangeState<PlayerWalkState>();
                }
                else
                {
                    _player.ChangeState<PlayerFlopState>();
                }

                return;
            }
        }

        private void LeftStickControls(float delta)
        {
            RotateTowardLeftStick(Mathf.DegToRad(Player.Roll.TurnSpeedDeg), delta);
            RedirectFSpeedTowardYaw();
        }

        private float TargetSpeed()
        {
            float t = _timer;

            if (t <= Player.Roll.FrictionlessDuration)
                return Player.Roll.InitialSpeed;

            t -= Player.Roll.FrictionlessDuration;

            if (t <= Player.Roll.FrictionDuration)
                return Player.Roll.MinSpeed;

            return Player.Roll.HoldSpeed;
        }
    }
}

