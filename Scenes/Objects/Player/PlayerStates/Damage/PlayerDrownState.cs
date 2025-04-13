using Godot;

namespace FastDragon
{
    public partial class PlayerDrownState : PlayerState
    {
        public override bool Invincible => true;

        private const float DrownDuration = 4;
        private float _timer;

        public override void OnStateEntered()
        {
            _timer = DrownDuration;
            SaveFile.Current.PlayerHealth--;
            _player.Animator.Play("Drown");
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.JumpJustPressed(ev) && SaveFile.Current.PlayerHealth > 0)
            {
                _player.ChangeState<PlayerWalkJumpState>();
            }
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            float speed = 1f / DrownDuration;
            _player.GlobalPosition += Vector3.Down * speed * delta;

            _timer -= delta;
            if (_timer <= 0)
                MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        }
    }
}