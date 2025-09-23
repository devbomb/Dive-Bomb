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

        public override void OnStateEntered(IState oldState)
        {
            Self.Animator.Play(RunAnim);

            if (Self.Velocity.Length() < Player.Walk.MinSpeed)
                Self.FSpeed = Player.Walk.MinSpeed;

            _sideFlipDisableTimer = Player.Walk.MinTimeBeforeSideFlip;
            _sideFlipWindowTimer = 0;

            bool canBound = (oldState as PlayerState)?.CanBoundAfterLanding ?? false;
            _boundJumpWindowTimer = canBound
                ? Player.BoundJump.TimeWindow
                : 0;

            // Let the player jump if they pressed the button a little bit too
            // early.
            //
            // Note that this does NOT update the last safe grounded position.
            // That's intentional!
            // The player can use this to delay setting their last safe position,
            // effectively using it as a "remote teleport" by jumping into the
            // water.
            if (Self.EarlyJumpBufferTimer > 0)
            {
                Jump();
            }
        }

        public override void OnStateExited()
        {
            ResetModelPitch();
            Self.Animator.SpeedScale = 1;
            Self.Model.Position = Vector3.Zero;
        }

        public override void _Process(double deltaD)
        {
            // Adjust the animation speed to match our actual speed
            float animLen = (float)Self.Animator.CurrentAnimationLength;
            float distancePerCycle = StrideLength * 2;
            float speed = Self.Velocity.Length();
            float speedScale = speed * animLen / distancePerCycle;
            Self.Animator.SpeedScale = speedScale;

            // Add a little "bounce" to the step.
            // This isn't part of the animation because its height needs to vary
            // according to the speed
            float height = BounceHeight / speedScale;
            float interval = (float)Self.Animator.CurrentAnimationLength / 2;
            float t = (float)(Self.Animator.CurrentAnimationPosition / interval);
            Self.Model.Position = Vector3.Up * height * Parabola(t);

            // Angle the model pitch by our speed
            Self.ModelPitchRad = -Mathf.LerpAngle(
                0,
                Player.Walk.MaxRunTiltRad,
                Mathf.Min(speed / Player.Walk.Speed, 1)
            );
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
                Self.ChangeState<PlayerRollState>();
                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                Self.ChangeState<PlayerKickState>();
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
            Self.MoveAndSlide();
            Self.SafeGround.UpdateLastSafeGroundPos();

            PlaySkidAnimIfTurningHard();

            if (!Self.IsOnFloor())
            {
                Self.ChangeState<PlayerFlopState>();
                return;
            }

            if (Self.Velocity.Length() < Player.Walk.MinSpeed)
            {
                Self.ChangeState<PlayerStandState>();
                return;
            }
        }

        private void Jump()
        {
            if (_sideFlipWindowTimer > 0 && _sideFlipDisableTimer <= 0)
                Self.ChangeState<PlayerSideFlipState>();
            else if (_boundJumpWindowTimer > 0)
                Self.ChangeState<PlayerBoundJumpState>();
            else
                Self.ChangeState<PlayerWalkJumpState>();
        }

        private void PlaySkidAnimIfTurningHard()
        {
            float leftStickForwardComponent = LeftStick3D().ComponentAlong(Self.GlobalForward());
            bool playingSkid = Self.Animator.AssignedAnimation == SkidAnim;

            if (leftStickForwardComponent < 0 && !playingSkid)
            {
                Self.Animator.Play(SkidAnim);
                _sideFlipWindowTimer = Player.SideFlip.TimeWindow;
            }

            if (leftStickForwardComponent >= 0 && playingSkid)
            {
                Self.Animator.Play(RunAnim);
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

