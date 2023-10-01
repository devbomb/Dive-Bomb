using Godot;

namespace FastDragon
{
    public partial class PlayerFallingToDeathState : PlayerState
    {
        public const float FallDuration = 1;

        private float _timer = 0;

        public override void OnStateEntered()
        {
            _player.Animator.Play("Flop");
            _timer = FallDuration;
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