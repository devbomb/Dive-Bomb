using Godot;

namespace FastDragon
{
    public abstract partial class PlayerJumpState : PlayerState
    {
        public virtual float RotSpeedRad => Player.Jump.RotSpeedRad;
        public virtual float MaxFSpeed => Player.Jump.MaxFSpeed;
        public virtual float StrafeAccel => Player.Jump.StrafeAccel;

        public virtual float InitVSpeed => Player.Jump.InitVSpeed;
        public virtual float FullJumpRiseGravity => Player.Jump.FullJumpRiseGravity;
        public virtual float ShortHopGravity => Player.Jump.ShortHopGravity;
        public virtual float FallGravity => Player.Default.Gravity;

        public virtual string AnimationName => "Jump";

        private bool _isHolding;

        public override void OnStateEntered()
        {
            Self.Animator.Play(AnimationName, 0);
            Self.VSpeed = InitVSpeed;

            _isHolding = true;
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.RollJustPressed(ev))
            {
                Self.ChangeState<PlayerDiveState>();
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

            RotateTowardLeftStick(RotSpeedRad, delta);
            AccelerateWithLeftStick(MaxFSpeed, StrafeAccel, delta);

            if (!InputService.JumpHeld)
                _isHolding = false;

            float gravity = FallGravity;
            if (Self.VSpeed > 0)
            {
                gravity = _isHolding
                    ? FullJumpRiseGravity
                    : ShortHopGravity;
            }

            ApplyGravity(delta, gravity);
            Self.MoveAndSlide();

            if (Self.IsOnFloor())
            {
                // Notice that we're not changing the VSpeed.
                // IsOnFloor() does not work reliably if you have 0 or positive
                // VSpeed, so we need to maintain a little bit of negative speed
                // to ensure it always returns true.
                StartWalkingOrStanding();
                return;
            }

            if (Self.IsOnWall() && Self.VSpeed < 0)
            {
                Self.ChangeState<PlayerWallSlideState>();
                return;
            }
        }
    }
}