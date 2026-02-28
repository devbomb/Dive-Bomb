using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeGrabState : PlayerState
    {
        private Vector3 _lastLedgePos;
        private StaticBody3D _currentLedge;

        public override void OnStateEntered()
        {
            Self.Animator.Play("GrabLedge");
            Self.LocalVelocity = Vector3.Zero;

            _currentLedge = Self.LedgeDetector.LastLedge;
            _lastLedgePos = _currentLedge.GlobalPosition;

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
            // Let go if the ledge no longer exists (or just isn't in the tree)
            if (!Node.IsInstanceValid(_currentLedge) || !_currentLedge.IsInsideTree())
            {
                GD.Print("Letting go of ledge because it no longer exists or is no longer in the tree");
                ChangeState<PlayerFlopState>();
                return;
            }

            // Move with the ledge
            Self.LastPlatformVelocity = (_currentLedge.GlobalPosition - _lastLedgePos) / (float)delta;
            Self.LastPlatformVelocity += _currentLedge.ConstantLinearVelocity;
            Self.LocalVelocity = Vector3.Zero;
            Self.MoveAndSlide();
            _lastLedgePos = _currentLedge.GlobalPosition;

            // Let go if we no longer meet the ledge grab criteria
            Self.LedgeDetector.ForceUpdate();
            if (!Self.LedgeDetector.LedgeDetected || Self.LedgeDetector.IsBlocked)
            {
                GD.Print("Letting go of ledge because it isn't detected anymore or is blocked");
                ChangeState<PlayerFlopState>();
                return;
            }

            // If a different ledge has been detected, switch to tracking it
            // instead.
            if (Self.LedgeDetector.LastLedge != _currentLedge)
            {
                GD.Print("Switching to a different ledge");
                _currentLedge = Self.LedgeDetector.LastLedge;
                _lastLedgePos = _currentLedge.GlobalPosition;
            }
        }
    }
}