using Godot;

namespace FastDragon
{
    public partial class PlayerWallJumpState : PlayerState
    {
        private float _timer;
        private bool _isHolding;

        public override void OnStateEntered()
        {
            RotateToFaceAwayFromWall();
            _player.ResetPhysicsInterpolation();

            _player.Animator.Play("Jump", 0);
            _player.VSpeed = Player.Jump.InitVSpeed;
            _player.FSpeed = Player.WallJump.FSpeed;

            _timer = 0;
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

            // Disable horizontal controls until the minimum duration has
            // passed, to prevent the player from scaling walls of indefinite
            // height
            _timer += delta;

            if (_timer > Player.WallJump.DisableStrafeDuration)
            {
                RotateTowardLeftStick(Player.Jump.RotSpeedRad, delta);
                AccelerateWithLeftStick(
                    Player.Jump.MaxFSpeed,
                    Player.Jump.StrafeAccel,
                    delta
                );
            }


            if (!InputService.JumpHeld)
                _isHolding = false;

            float gravity = Player.Default.Gravity;
            if (_player.VSpeed > 0)
            {
                gravity = _isHolding
                    ? Player.Jump.FullJumpRiseGravity
                    : Player.Jump.ShortHopGravity;
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

        private void RotateToFaceAwayFromWall()
        {
            if (!_player.IsOnWall())
            {
                GD.PushWarning("Wall jump was triggered while not touching a wall");
                return;
            }

            _player.GlobalRotation = _player.GetWallNormal()
                .Flattened()
                .ForwardToEulerAnglesRad();
        }
    }
}