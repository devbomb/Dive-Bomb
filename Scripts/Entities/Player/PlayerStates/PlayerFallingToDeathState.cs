using Godot;

namespace FastDragon
{
    public partial class PlayerFallingToDeathState : PlayerState
    {
        public const float FallDuration = 1;

        private float _timer = 0;
        private Vector3 _initialCameraPos;

        public override void OnStateEntered()
        {
            _player.Animator.Play("Flop");
            _timer = FallDuration;
            _player.Camera.ChangeState<OrbitCameraLockedState>();
            _initialCameraPos = _player.Camera.GlobalPosition;
        }

        public override void _Process(double delta)
        {
            _player.Camera.GlobalPosition = _initialCameraPos;
            _player.Camera.LookAt(_player.GlobalPosition);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _timer -= delta;
            _player.Velocity += Vector3.Down * Player.Default.Gravity * delta;
            _player.MoveAndSlide();

            if (_timer <= 0)
                SignalBus.Instance.EmitLevelReset();
        }
    }
}