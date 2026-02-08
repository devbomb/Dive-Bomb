using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeGrabState : PlayerState
    {
        public override void OnStateEntered()
        {
            Self.Animator.Play("GrabLedge");
            Self.LocalVelocity = Vector3.Zero;

            // TODO: Set LastPlatformVelocity to the ledge's velocity.
            // And make the player travel with the ledge as it moves.

            // Snap to the correct height.
            // The height should be such that the ledge grab point is at exactly
            // the ledge height.
            var pos = Self.GlobalPosition;
            pos.Y = Self.LedgeDetector.LedgeGlobalY;
            pos.Y -= Self.LedgeGrabPoint.Position.Y;
            Self.GlobalPosition = pos;

            // Rotate to face the wall.
            // It would be weird otherwise.
            Self.GlobalRotation = (-Self.GetWallNormal())
                .Flattened()
                .ForwardToEulerAnglesRad();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                if (Self.LedgeDetector.LastLedgePointRequiresSafeClimb)
                    ChangeState<PlayerLedgeClimbSafeState>();
                else
                    ChangeState<PlayerLedgeClimbState>();

                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                Self.ChangeState<PlayerKickState>();
                return;
            }
        }
    }
}