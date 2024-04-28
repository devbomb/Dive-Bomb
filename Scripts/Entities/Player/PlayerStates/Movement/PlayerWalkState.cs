using Godot;
using System;

namespace FastDragon
{
    public partial class PlayerWalkState : PlayerState
    {
        private const string RunAnim = "Run";
        private const string SkidAnim = "Skid";

        private const float StrideLength = 1;
        private const float BounceHeight = 0.1f;

        private float _sideFlipDisableTimer;
        private float _sideFlipWindowTimer;
        private float _boundJumpWindowTimer;

        public override void OnStateEntered(State oldState)
        {
            _player.Animator.Play(RunAnim);

            if (_player.Velocity.Length() < Player.Walk.MinSpeed)
                _player.FSpeed = Player.Walk.MinSpeed;

            _sideFlipDisableTimer = Player.Walk.MinTimeBeforeSideFlip;
            _sideFlipWindowTimer = 0;

            bool canBound = (oldState as PlayerState)?.CanBoundAfterLanding ?? false;
            _boundJumpWindowTimer = canBound
                ? Player.BoundJump.TimeWindow
                : 0;

            // Let the player jump if they pressed the button a little bit too
            // early
            if (_player.EarlyJumpBufferTimer > 0)
            {
                Jump();
            }
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
            _player.Animator.SpeedScale = 1;
            _player.Model.Position = Vector3.Zero;
        }

        public override void _Process(double deltaD)
        {
            float delta = (float)deltaD;

            AngleModelPitchWithGroundSlope(delta);

            // Adjust the animation speed to match our actual speed
            float animLen = (float)_player.Animator.CurrentAnimationLength;
            float distancePerCycle = StrideLength * 2;
            float speed = _player.Velocity.Length();
            float speedScale = speed * animLen / distancePerCycle;
            _player.Animator.SpeedScale = speedScale;

            // Add a little "bounce" to the step.
            // This isn't part of the animation because its height needs to vary
            // according to the speed
            float height = BounceHeight / speedScale;
            float interval = (float)_player.Animator.CurrentAnimationLength / 2;
            float t = (float)(_player.Animator.CurrentAnimationPosition / interval);
            _player.Model.Position = Vector3.Up * height * Parabola(t);
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                Jump();
                return;
            }

            if (InputService.RollJustPressed(ev))
            {
                _player.ChangeState<PlayerRollState>();
                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                _player.ChangeState<PlayerKickState>();
                return;
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _sideFlipDisableTimer -= delta;
            _sideFlipWindowTimer -= delta;
            _boundJumpWindowTimer -= delta;

            StrafeWithLeftStick(Player.Walk.Speed, Player.Walk.Accel, delta);
            RotateInstantlyTowardVelocity();
            _player.MoveAndSlide();

            PlaySkidAnimIfTurningHard();

            if (!_player.IsOnFloor())
            {
                _player.ChangeState<PlayerFlopState>();
                return;
            }

            if (_player.Velocity.Length() < Player.Walk.MinSpeed)
            {
                _player.ChangeState<PlayerStandState>();
                return;
            }
        }

        private void Jump()
        {
            if (_sideFlipWindowTimer > 0 && _sideFlipDisableTimer <= 0)
                _player.ChangeState<PlayerSideFlipState>();
            else if (_boundJumpWindowTimer > 0)
                _player.ChangeState<PlayerBoundJumpState>();
            else
                _player.ChangeState<PlayerWalkJumpState>();
        }

        private void PlaySkidAnimIfTurningHard()
        {
            float leftStickForwardComponent = LeftStick3D().ComponentAlong(_player.GlobalForward());
            bool playingSkid = _player.Animator.AssignedAnimation == SkidAnim;

            if (leftStickForwardComponent < 0 && !playingSkid)
            {
                _player.Animator.Play(SkidAnim);
                _sideFlipWindowTimer = Player.SideFlip.TimeWindow;
            }

            if (leftStickForwardComponent >= 0 && playingSkid)
            {
                _player.Animator.Play(RunAnim);
            }
        }

        private float Parabola(float t)
        {
            t %= 1f;
            float x = (2 * t) - 1;
            return 1 - (x * x);
        }
    }
}

