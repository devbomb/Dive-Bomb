using Godot;

namespace FastDragon
{
    public partial class PlayerWallSlideState : PlayerState
    {
        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                _player.ChangeState<PlayerWallJumpState>();
                return;
            }
        }

        public override void OnStateEntered()
        {
            _player.Animator.Play("WallSlide");
            RotateToFaceWall();

            if (_player.EarlyJumpBufferTimer > 0)
            {
                _player.ChangeState<PlayerWallJumpState>();
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            // Apply friction to the horizontal speed.
            // We do need to keep _some_ velocity going _into_ the wall, though,
            // to ensure IsOnWall() still works reliably.
            var targetFlatVel = -_player.GetWallNormal()
                .Flattened()
                .Normalized() * 0.1f;

            var flatVel = _player.Velocity.Flattened();
            flatVel = flatVel.MoveToward(targetFlatVel, Player.WallSlide.HDecel * delta);
            flatVel.Y = _player.VSpeed;
            _player.Velocity = flatVel;

            ApplyGravity(delta, Player.WallSlide.Gravity);
            _player.VSpeed = Mathf.Max(_player.VSpeed, -Player.WallSlide.TerminalVelocity);

            _player.MoveAndSlide();

            if (TryGrabLedge())
                return;

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            if (!_player.IsOnWall())
            {
                _player.ChangeState<PlayerFlopState>();
                return;
            }

            RotateToFaceWall();
        }

        private void RotateToFaceWall()
        {
            if (!_player.IsOnWall())
            {
                throw new System.Exception("Tried to rotate to the wall, but not touching one");
            }

            _player.GlobalRotation = (-_player.GetWallNormal())
                .Flattened()
                .ForwardToEulerAnglesRad();
        }
    }
}