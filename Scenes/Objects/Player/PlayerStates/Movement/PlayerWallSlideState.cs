using Godot;

namespace FastDragon
{
    public partial class PlayerWallSlideState : PlayerState
    {
        private Vector3 _lastWallNormal;

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                TryWallJump();
                return;
            }
        }

        public override void OnStateEntered()
        {
            _player.Animator.Play("WallSlide");

            _lastWallNormal = _player.GetWallNormal();
            RotateToFaceWall();

            if (_player.EarlyJumpBufferTimer > 0)
            {
                TryWallJump();
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            AccelerateWithLeftStick(
                Player.Jump.MaxFSpeed,
                Player.Jump.StrafeAccel,
                delta
            );

            ApplyGravity(delta, Player.Default.Gravity);
            _player.MoveAndSlide();

            if (TryGrabLedge())
                return;

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            if (!StillOnWall())
            {
                _player.ChangeState<PlayerFlopState>();
                return;
            }

            RotateToFaceWall();
        }

        private bool StillOnWall()
        {
            var collision = new KinematicCollision3D();
            bool onWall = _player.TestMove(
                _player.GlobalTransform,
                -_lastWallNormal * 0.01f,
                collision,
                recoveryAsCollision: true
            );

            if (onWall)
            {
                _lastWallNormal = collision.GetNormal();
                GD.Print($"Wall normal: {_lastWallNormal}");
            }

            return onWall;
        }

        private void RotateToFaceWall()
        {
            _player.GlobalRotation = (-_lastWallNormal)
                .Flattened()
                .ForwardToEulerAnglesRad();
        }

        private void RotateToFaceAwayFromWall()
        {
            _player.GlobalRotation = _lastWallNormal
                .Flattened()
                .ForwardToEulerAnglesRad();
        }

        private void TryWallJump()
        {
            // Don't allow the player to wall jump if the wall is angled down
            // at all.
            //
            // This allows level designers to prevent the player from wall
            // jumping by simply slanting the walls.
            if (_player.GetWallNormal().Y >= 0)
            {
                RotateToFaceAwayFromWall();
                _player.ChangeState<PlayerWallJumpState>();
            }
        }
    }
}