using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeGrabState : PlayerState
    {
        private Vector3 _lastLedgePos;
        private StaticBody3D _lastLedge;

        public override void OnStateEntered()
        {
            Self.Animator.Play("GrabLedge");
            Self.LocalVelocity = Vector3.Zero;

            // TODO: Set LastPlatformVelocity to the ledge's velocity.
            // And make the player travel with the ledge as it moves.
            _lastLedge = Self.LedgeDetector.LastLedge;
            _lastLedgePos = _lastLedge.GlobalPosition;

            // Snap to the correct height.
            // The height should be such that the ledge grab point is at exactly
            // the ledge height.
            var pos = Self.GlobalPosition;
            pos.Y = Self.LedgeDetector.LedgeGlobalY;
            pos.Y -= Self.LedgeGrabPoint.Position.Y;
            Self.GlobalPosition = pos;

            // Because the player is a sphere, changing their height probably
            // made them clip into the wall a little bit.  Let's move them out.
            //
            // Don't believe me?  Imagine a billiard ball teetering on the edge
            // of a cliff.  If you just move that ball straight down, it would
            // clip into that cliff, wouldn't it?
            Self.MoveAndCollide(Vector3.Zero);

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

        public override void _PhysicsProcess(double delta)
        {
            Self.LastPlatformVelocity = (_lastLedge.GlobalPosition - _lastLedgePos) / (float)delta;
            Self.LocalVelocity = Vector3.Zero;
            Self.MoveAndSlide();

            _lastLedgePos = _lastLedge.GlobalPosition;

            // TODO: Let go of the ledge if we're no longer meeting the ledge
            // grab conditions.
        }
    }
}