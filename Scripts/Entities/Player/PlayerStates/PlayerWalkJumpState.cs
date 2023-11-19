using Godot;

namespace FastDragon
{
    public partial class PlayerWalkJumpState : PlayerState
    {
        private const float GlideDebounceDuration = 0.1f;
        private const bool PrintMaxHeight = false;

        private float _holdJumpTimer;
        private float _glideDebounceTimer;
        private float _startY;
        private float _maxHeight;
        private bool _printedMaxHeight;

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
            _player.Animator.Play("Jump");
            _player.VSpeed = Player.Jump.InitVSpeed;

            _holdJumpTimer = Player.Jump.MaxHoldTime;
            _glideDebounceTimer = GlideDebounceDuration;

             _startY = _player.GlobalPosition.Y;
             _maxHeight = 0;
             _printedMaxHeight = false;
        }

        public override void _Input(InputEvent ev)
        {
            if (_glideDebounceTimer <= 0)
                GlideWithJumpButton(ev);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _holdJumpTimer -= delta;
            _glideDebounceTimer -= delta;

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

            // DEBUG: Print the max height
            float height = _player.GlobalPosition.Y - _startY;
            if (height > _maxHeight)
            {
                _maxHeight = height;
            }
            else if (!_printedMaxHeight && PrintMaxHeight)
            {
                _printedMaxHeight = true;
                GD.Print(_maxHeight);
            }

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