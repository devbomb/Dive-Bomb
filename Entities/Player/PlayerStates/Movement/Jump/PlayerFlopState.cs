using Godot;

namespace FastDragon
{
    public partial class PlayerFlopState : PlayerState
    {
        private float _coyoteTimer;

        public override void OnStateEntered(IState prevState)
        {
            Self.Animator.Play("Flop");

            if (prevState is PlayerWalkState)
                _coyoteTimer = Player.Default.CoyoteTime;
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev) && _coyoteTimer > 0)
            {
                GD.Print("Coyote jump!");
                Self.ChangeState<PlayerWalkJumpState>();
                return;
            }

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

            if (_coyoteTimer > 0)
                _coyoteTimer -= delta;

            RotateTowardLeftStick(Player.Jump.RotSpeedRad, delta);
            AccelerateWithLeftStick(
                Player.Jump.MaxFSpeed,
                Player.Jump.StrafeAccel,
                delta
            );

            ApplyGravity(delta, Player.Default.Gravity);
            Self.MoveAndSlide();

            if (Self.IsOnFloor())
            {
                Self.ChangeState<PlayerWalkState>();
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