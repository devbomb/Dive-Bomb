using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerRollState : PlayerState
    {
        private const float RollingRadius = 0.5f;
        private const float RollingCircumference = 2 * Mathf.Pi * RollingRadius;

        private float _timer;
        private bool _redirectingAllowed;

        public override void OnStateEntered(State oldState)
        {
            _player.Animator.Play("Roll");
            _player.Velocity = _player.GlobalForward() * Player.Roll.InitialSpeed;

            _timer = 0;
            _redirectingAllowed = !(oldState is PlayerDiveState);
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

            if (_redirectingAllowed && _timer < Player.Roll.RedirectTimeWindow)
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
            MoveAndSlideRolling(delta);

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
                if (body is IRollable f)
                {
                    f.OnRolledInto();
                }
            }

            foreach (var area in areas)
            {
                if (area is IRollable f)
                {
                    f.OnRolledInto();
                }
            }
        }
    }
}

