using Godot;
using System;
using System.Collections.Generic;

namespace FastDragon
{
    public partial class PlayerRollState : PlayerState
    {
        private const float RollingRadius = 0.5f;
        private const float RollingCircumference = 2 * Mathf.Pi * RollingRadius;

        private const float CameraLagDuration = 0.5f;

        private float _timer;
        private bool _isGroundRoll;

        private List<IBreakable> _brokenObjects = new List<IBreakable>();
        private List<IBreakable> _unbrokenObjects= new List<IBreakable>();

        private AudioStreamPlayer _rollSoundPlayer => _player.GetNode<AudioStreamPlayer>("%RollSoundPlayer");

        public override void OnStateEntered(State oldState)
        {
            _player.Animator.Play("Roll");

            _timer = 0;
            _isGroundRoll = !(oldState is PlayerDiveState);
            _player.Velocity = _player.GlobalForward() * Player.Roll.InitialSpeed;
            _player.Camera.Lag(CameraLagDuration);

            _rollSoundPlayer.Play(0.025f);
        }

        public override void OnStateExited()
        {
            _player.Animator.SpeedScale = 1;
        }

        public override void _Process(double deltaD)
        {
            float scale = _player.Velocity.Length() / RollingCircumference;
            _player.Animator.SpeedScale = scale;
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
            _unbrokenObjects.Clear();

            MoveAndSlideBreakingObjects<IBreakable>(
                isVulnerable: b => b.VulnerableToRoll,
                causesBonkWhenBroken: b => b.CausesBonk,
                brokenObjects: _brokenObjects,
                unbrokenObjects: _unbrokenObjects,
                delta
            );

            foreach (var b in _brokenObjects)
            {
                b.OnRolledInto();
                Break(b);
            }

            foreach (var b in _unbrokenObjects)
                b.OnBreakRejected();

            // TODO: Don't apply the extra hitbox to objects that have already
            // been hit by the main hitbox
            ApplyExtraHitbox();

            // It's possible for the objects hit by the hitbox to change the
            // current state.  We don't want to overwrite that state change if
            // it happened to happen on the final frame of the timer.
            if (!IsCurrent)
                return;

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
                if (body is IBreakable b && b.VulnerableToRoll)
                {
                    b.OnRolledInto();

                    if (b.VulnerableToRoll)
                        Break(b);
                    else
                        b.OnBreakRejected();
                }
            }

            foreach (var area in areas)
            {
                if (area is IBreakable b)
                {
                    b.OnRolledInto();

                    if (b.VulnerableToRoll)
                        Break(b);
                    else
                        b.OnBreakRejected();
                }
            }
        }

        private void Break(IBreakable b)
        {
            b.OnBroken();
            _player.Camera.Shake(
                b.CameraShakeMagnitude,
                b.CameraShakeFrequency,
                b.CameraShakeDuration
            );
        }
    }
}

