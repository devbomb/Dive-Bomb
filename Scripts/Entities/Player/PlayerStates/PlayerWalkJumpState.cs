using Godot;

namespace FastDragon
{
    public partial class PlayerWalkJumpState : PlayerState
    {
        private const float GlideDebounceDuration = 0.1f;
        private const bool PrintMaxHeight = true;

        private bool _isHolding;

        private float _glideDebounceTimer;
        private float _startY;
        private float _maxHeight;
        private bool _printedMaxHeight;

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
            _player.Animator.Play("Jump");
            _player.VSpeed = Player.Jump.InitVSpeed;

            _isHolding = true;

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

            _glideDebounceTimer -= delta;

            RotateTowardLeftStick(Mathf.DegToRad(Player.Jump.RotSpeedDeg), delta);
            StrafeWithLeftStick(
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
                    ? Player.Jump.FullJumpRiseGravity
                    : Player.Jump.ShortHopGravity;
            }

            ApplyGravity(delta, gravity);
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