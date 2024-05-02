using Godot;
using System;
using System.Collections.Generic;

namespace FastDragon
{
    public partial class PlayerRollState : PlayerState
    {
        private const float RollingRadius = 0.5f;
        private const float RollingCircumference = 2 * Mathf.Pi * RollingRadius;

        private const float CameraShakeMagnitude = 0.25f;
        private const float CameraShakeFrequency = 15;
        private const float CameraShakeDuration = 0.25f;

        private const float CameraLagDuration = 0.5f;

        private float _timer;
        private bool _isGroundRoll;

        private List<IRollable> _brokenObjects = new List<IRollable>();

        public override void OnStateEntered(State oldState)
        {
            _player.Animator.Play("Roll");

            _timer = 0;
            _isGroundRoll = !(oldState is PlayerDiveState);
            _player.Velocity = _player.GlobalForward() * Player.Roll.InitialSpeed;
            _player.Camera.Lag(CameraLagDuration);
        }

        public override void OnStateExited()
        {
            _player.Animator.SpeedScale = 1;
        }

        public override void _Process(double deltaD)
        {
            _player.Animator.SpeedScale = _player.Velocity.Length() / RollingCircumference;
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

            _timer += delta;

            if (_isGroundRoll && _timer < Player.Roll.RedirectTimeWindow)
            {
                float speed = _player.FSpeed;
                RotateInstantlyTowardLeftStick();
                _player.FSpeed = speed;
            }

            float maxSpeed = _timer < Player.Roll.FrictionlessDuration
                ? Player.Roll.InitialSpeed
                : Player.Roll.MinSpeed;

            AccelerateWithLeftStickAgainstDrag(
                maxSpeed,
                Player.Roll.MinAccel,
                Player.Roll.MaxAccel,
                delta
            );

            RotateInstantlyTowardVelocity();

            _brokenObjects.Clear();
            MoveAndSlideBreakingObjects<IRollable>(
                isBreakable: r => true,
                causesBonkWhenBroken: r => r.CausesBonk,
                _brokenObjects,
                delta
            );

            foreach (var r in _brokenObjects)
            {
                OnBroke(r);
            }

            // TODO: Don't apply the extra hitbox to objects that have already
            // been hit by the main hitbox
            ApplyExtraHitbox();

            if (_timer >= Player.Roll.Duration)
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

        private void ApplyExtraHitbox()
        {
            var bodies = _player.RollExtraHitbox.GetOverlappingBodies();
            var areas = _player.RollExtraHitbox.GetOverlappingAreas();

            foreach (var body in bodies)
            {
                if (body is IRollable r)
                {
                    OnBroke(r);
                }
            }

            foreach (var area in areas)
            {
                if (area is IRollable r)
                {
                    OnBroke(r);
                }
            }
        }

        private void OnBroke(IRollable r)
        {
            r.OnRolledInto();
            _player.Camera.Shake(
                CameraShakeMagnitude,
                CameraShakeFrequency,
                CameraShakeDuration
            );
        }
    }
}

