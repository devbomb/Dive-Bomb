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
            _player.VSpeed = Player.Default.JumpVSpeed;
            _holdJumpTimer = Player.Default.MaxJumpHoldTime;
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev))
            {
                _player.ChangeState<PlayerGlideState>();
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _holdJumpTimer -= delta;

            if (!InputService.JumpHeld)
                _holdJumpTimer = 0;

            RotateTowardLeftStick(Mathf.DegToRad(Player.Walk.RotSpeedDeg), delta);
            AccelerateWithLeftStick(
                Player.Walk.Speed,
                Player.Walk.Accel,
                Player.Walk.Decel,
                delta
            );

            ApplyGravity(
                delta,
                _holdJumpTimer > 0
                    ? Player.Default.JumpHoldGravity
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