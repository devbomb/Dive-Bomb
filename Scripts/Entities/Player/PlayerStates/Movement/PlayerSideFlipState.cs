using Godot;

namespace FastDragon
{
    public partial class PlayerSideFlipState : PlayerState
    {
        public override bool CanBoundAfterLanding => true;

        private bool _isHolding;

        public override void OnStateEntered()
        {
            _player.Animator.Play("SideFlip", 0);
            _player.VSpeed = Player.SideFlip.InitVSpeed;

            RotateInstantlyTowardLeftStick();
            _player.FSpeed = Player.Walk.Speed;
            _player.ResetPhysicsInterpolation();

            _isHolding = true;
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

            if (!InputService.JumpHeld)
                _isHolding = false;

            float gravity = Player.Default.Gravity;
            if (_player.VSpeed > 0)
            {
                gravity = _isHolding
                    ? Player.SideFlip.FullJumpRiseGravity
                    : Player.SideFlip.ShortHopGravity;
            }

            ApplyGravity(delta, gravity);
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