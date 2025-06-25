using Godot;

namespace FastDragon
{
    public partial class PlayerSideFlipState : PlayerState
    {
        public override bool CanBoundAfterLanding => true;

        private bool _isHolding;

        public override void OnStateEntered()
        {
            Self.Animator.Play("SideFlip", 0);
            Self.VSpeed = Player.SideFlip.InitVSpeed;

            RotateInstantlyTowardLeftStick();
            Self.FSpeed = Player.Walk.Speed;
            Self.ResetPhysicsInterpolation3D();

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
                    ? Player.SideFlip.FullJumpRiseGravity
                    : Player.SideFlip.ShortHopGravity;
            }

            ApplyGravity(delta, gravity);
            Self.MoveAndSlide();

            if (Self.IsOnFloor())
            {
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