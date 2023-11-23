using Godot;

namespace FastDragon
{
    public partial class PlayerDrownState : PlayerState
    {
        private const float DrownDuration = 4;
        private float _timer;

        public override void OnStateEntered()
        {
            _timer = DrownDuration;
            _player.Animator.Play("Drown");
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