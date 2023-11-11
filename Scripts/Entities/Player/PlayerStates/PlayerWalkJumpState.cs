using Godot;

namespace FastDragon
{
    public partial class PlayerWalkJumpState : PlayerState
    {
        private float _holdJumpTimer;

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
            _player.Animator.Play("Jump");
            _player.VSpeed = Player.Jump.InitVSpeed;
            _holdJumpTimer = Player.Jump.MaxHoldTime;
        }

        public override void _Input(InputEvent ev)
        {
            GlideWithJumpButton(ev);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _holdJumpTimer -= delta;

            if (!InputService.JumpHeld)
                _holdJumpTimer = 0;

            RotateTowardLeftStick(Mathf.DegToRad(Player.Jump.RotSpeedDeg), delta);
            StrafeWithLeftStick(
                Player.Jump.MaxFSpeed,
                Player.Jump.StrafeAccel,
                delta
            );

            ApplyGravity(
                delta,
                _holdJumpTimer > 0
                    ? Player.Jump.HoldGravity
                    : Player.Default.Gravity
            );

            _player.MoveAndSlide();

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            // Charge when the button is held
            if (InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerChargeFallState>();
                return;
            }
        }
    }
}