using Godot;

namespace FastDragon
{
    public partial class PlayerKickFlopState : PlayerState
    {
        public override void OnStateEntered()
        {
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.RollJustPressed(ev))
            {
                Self.ChangeState<PlayerDiveState>();
                return;
            }

            // We intentionally do not kicking again from this state, to
            // prevent the player from combining wall jumps with kicks to
            // climb infinitely high.
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

            ApplyGravity(delta, Player.Default.Gravity);
            Self.MoveAndSlide();

            if (Self.IsOnFloor())
            {
                Self.ChangeState<PlayerWalkState>();
                return;
            }

            if (TryGrabLedge())
                return;

            // We intentionally do not allow wall sliding from this state, to
            // prevent the player from combining wall jumps with kicks to
            // climb infinitely high.
        }
    }
}