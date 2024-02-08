using Godot;

namespace FastDragon
{
    public partial class PlayerFlopState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Animator.Play("Flop");
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.RollJustPressed(ev))
            {
                _player.ChangeState<PlayerDiveState>();
                return;
            }

            if (InputService.KickJustPressed(ev))
            {
                _player.ChangeState<PlayerKickState>();
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

            ApplyGravity(delta, Player.Default.Gravity);
            _player.MoveAndSlide();

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            if (TryGrabLedge())
                return;

            if (_player.IsOnWall() && _player.VSpeed < 0)
            {
                _player.ChangeState<PlayerWallSlideState>();
                return;
            }
        }
    }
}