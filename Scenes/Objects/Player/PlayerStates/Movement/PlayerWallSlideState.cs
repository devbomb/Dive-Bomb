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
            Self.Animator.Play("WallSlide");

            _lastWallNormal = Self.GetWallNormal();
            RotateToFaceWall();

            if (Self.EarlyJumpBufferTimer > 0)
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
            Self.MoveAndSlide();

            if (TryGrabLedge())
                return;

            if (Self.IsOnFloor())
            {
                Self.ChangeState<PlayerWalkState>();
                return;
            }

            if (!StillOnWall())
            {
                Self.ChangeState<PlayerFlopState>();
                return;
            }

            RotateToFaceWall();
        }

        private bool StillOnWall()
        {
            var collision = new KinematicCollision3D();
            bool onWall = Self.TestMove(
                Self.GlobalTransform,
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
            Self.GlobalRotation = (-_lastWallNormal)
                .Flattened()
                .ForwardToEulerAnglesRad();
        }

        private void RotateToFaceAwayFromWall()
        {
            Self.GlobalRotation = _lastWallNormal
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
            if (Self.GetWallNormal().Y >= 0)
            {
                RotateToFaceAwayFromWall();
                Self.ChangeState<PlayerWallJumpState>();
            }
        }
    }
}