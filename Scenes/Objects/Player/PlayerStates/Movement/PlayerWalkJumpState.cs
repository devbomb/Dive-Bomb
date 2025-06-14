using Godot;

namespace FastDragon
{
    public partial class PlayerWalkJumpState : PlayerState
    {
        public override bool CanBoundAfterLanding => true;

        private bool _isHolding;

        public override void OnStateEntered()
        {
            Self.Animator.Play("Jump", 0);
            Self.VSpeed = Player.Jump.InitVSpeed;

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

            RotateTowardLeftStick(Player.Jump.RotSpeedRad, delta);
            AccelerateWithLeftStick(
                Player.Jump.MaxFSpeed,
                Player.Jump.StrafeAccel,
                delta
            );

            if (!InputService.JumpHeld)
                _isHolding = false;

            float gravity = Player.Default.Gravity;
            if (Self.VSpeed > 0)
            {
                gravity = _isHolding
                    ? Player.Jump.FullJumpRiseGravity
                    : Player.Jump.ShortHopGravity;
            }

            ApplyGravity(delta, gravity);
            Self.MoveAndSlide();

            if (Self.IsOnFloor())
            {
                // Notice that we're not changing the VSpeed.
                // IsOnFloor() does not work reliably if you have 0 or positive
                // VSpeed, so we need to maintain a little bit of negative speed
                // to ensure it always returns true.
                Self.ChangeState<PlayerWalkState>();
                return;
            }

            if (TryGrabLedge())
                return;

            if (Self.IsOnWall() && Self.VSpeed < 0)
            {
                Self.ChangeState<PlayerWallSlideState>();
                return;
            }
        }
    }
}