using Godot;

namespace FastDragon
{
    public partial class PlayerWalkJumpState : PlayerState
    {
        private bool _rising = false;

        public override void OnStateEntered()
        {
            _player.Camera.ChangeState<OrbitCameraFreeState>();
            SetVSpeed(Player.Default.JumpVSpeed);
            _rising = true;
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

            if (_player.Velocity.Y <= 0 || !InputService.JumpHeld)
                _rising = false;

            WalkControls(Player.Walk.Speed, Player.Walk.Accel, delta);
            ApplyGravity(
                delta,
                _rising
                    ? Player.Default.JumpRiseGravity
                    : Player.Default.Gravity
            );

            _player.MoveAndSlide();

            if (_player.IsOnFloor())
            {
                _player.ChangeState<PlayerWalkState>();
                return;
            }

            // Charge when the button is held
            // TODO: Go to the charge fall state instead
            if (InputService.ChargeHeld)
            {
                _player.ChangeState<PlayerChargeState>();
                return;
            }
        }
    }
}