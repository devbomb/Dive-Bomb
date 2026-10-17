using Godot;

namespace FastDragon
{
    public partial class PlayerWallJumpState : PlayerState
    {
        public override bool CanBoundAfterLanding => true;

        private float _timer;
        private bool _isHolding;

        public override void OnStateEntered()
        {
            Self.ResetPhysicsInterpolation3D();

            Self.Animator.Play("Jump", 0);
            Self.VSpeed = Player.Jump.InitVSpeed;
            Self.FSpeed = Player.WallJump.FSpeed;

            _timer = 0;
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

            // Disable horizontal controls until the minimum duration has
            // passed, to prevent the player from scaling walls of indefinite
            // height
            _timer += delta;

            if (_timer > Player.WallJump.DisableStrafeDuration)
            {
                RotateTowardLeftStick(Player.Jump.RotSpeedRad, delta);
                AccelerateWithLeftStick(
                    Player.Jump.MaxFSpeed,
                    Player.Jump.StrafeAccel,
                    delta
                );
            }


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