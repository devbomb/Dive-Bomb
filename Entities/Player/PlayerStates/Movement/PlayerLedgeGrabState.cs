using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeGrabState : PlayerState
    {
        private LedgeDetector.DetectedLedge _currentLedge;

        public override void OnStateEntered()
        {
            Self.Animator.Play("GrabLedge");
            Self.LocalVelocity = Vector3.Zero;

            _currentLedge = Self.LedgeDetector.DetectLedge().Value;

            // Snap into position.
            Self.GlobalPosition = _currentLedge.HangingPosition;

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
                if (_currentLedge.RequiresSafeClimb)
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
            if (!Node.IsInstanceValid(_currentLedge.LedgeBody))
            {
                GD.Print("Letting go of ledge because it no longer exists");
                ChangeState<PlayerFlopState>();
                return;
            }

            if (!_currentLedge.LedgeBody.IsInsideTree())
            {
                GD.Print("Letting go of ledge because it is no longer in the tree");
                ChangeState<PlayerFlopState>();
                return;
            }

            // Move with the ledge
            Self.LastPlatformVelocity = _currentLedge.LedgePlatformVelocity;
            Self.LocalVelocity = Vector3.Zero;
            Self.MoveAndSlide();

            // Re-check the ledge status.
            // Let go if no ledge is detected anymore.
            var updatedLedge = Self.LedgeDetector.DetectLedge();
            if (!updatedLedge.HasValue)
            {
                GD.Print("Letting go of ledge because it isn't detected anymore");
                ChangeState<PlayerFlopState>();
                return;
            }

            _currentLedge = updatedLedge.Value;

            // Let go if the path to climb up the ledge is now blocked
            if (_currentLedge.IsClimbingPathBlocked)
            {
                GD.Print("Letting go of ledge because the path to climb up it is blocked");
                ChangeState<PlayerFlopState>();
                return;
            }
        }
    }
}