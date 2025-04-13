using Godot;

namespace FastDragon
{
    public partial class PlayerLedgeGrabState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Animator.Play("GrabLedge");
            _player.Velocity = Vector3.Zero;

            // Snap to the correct height.
            // The height should be such that the ledge grab point is at exactly
            // the ledge height.
            var pos = _player.GlobalPosition;
            pos.Y = _player.LedgeDetector.LedgeHeight;
            pos.Y -= _player.LedgeGrabPoint.Position.Y;
            _player.GlobalPosition = pos;

            // Rotate to face the wall.
            // It would be weird otherwise.
            _player.GlobalRotation = (-_player.GetWallNormal())
                .Flattened()
                .ForwardToEulerAnglesRad();
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                _player.ChangeState<PlayerLedgeClimbState>();
                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                _player.ChangeState<PlayerKickState>();
                return;
            }
        }
    }
}