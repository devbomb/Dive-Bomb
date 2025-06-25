using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeClimbState : PlayerState
    {
        private float _endHeight;

        public override void OnStateEntered()
        {
            float climbAnimLen = Self.Animator.GetAnimation("Roll").Length;
            Self.Animator.Play(
                "Roll",
                customBlend: 0.125f,
                customSpeed: climbAnimLen / 0.5f
            );

            Self.VSpeed = Player.LedgeClimb.InitVSpeed;
            Self.FSpeed = Player.LedgeClimb.FSpeed;

            _endHeight = Self.LedgeGrabPoint.GlobalPosition.Y;
        }

        public override void OnStateExited()
        {
            Self.Animator.Play("RESET", customBlend: 0);
            Self.Animator.Advance(0);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            if (Self.GlobalPosition.Y < _endHeight)
                Self.FSpeed = Player.LedgeClimb.FSpeed;
            else
            {
                RotateTowardLeftStick(Player.Jump.RotSpeedRad, delta);
                AccelerateWithLeftStick(
                    Player.Jump.MaxFSpeed,
                    Player.Jump.StrafeAccel,
                    delta
                );
            }

            ApplyGravity(delta, Player.LedgeClimb.Gravity);
            Self.MoveAndSlide();

            if (Self.IsOnFloor())
                Self.ChangeState<PlayerWalkState>();
        }
    }
}