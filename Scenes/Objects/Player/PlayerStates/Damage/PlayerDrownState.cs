using Godot;

namespace FastDragon
{
    public partial class PlayerDrownState : PlayerState
    {
        public override bool Invincible => true;

        private const float DrownDuration = 1;
        private float _timer;

        public override void OnStateEntered()
        {
            _timer = DrownDuration;
            SaveFile.Current.PlayerHealth--;
            _player.Animator.Play("Drown");
            _player.GetNode<AudioStreamPlayer>("%DrownStartSplashSound").Play();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            float speed = 1f / DrownDuration;
            _player.GlobalPosition += Vector3.Down * speed * delta;

            _timer -= delta;
            if (_timer <= 0)
            {
                if (SaveFile.Current.PlayerHealth > 0)
                    ReturnToLastSafePlace();
                else
                    MapTransitionManager.Instance.RespawnPlayerAfterDeath();
            }
        }

        private void ReturnToLastSafePlace()
        {
            _player.GlobalTransform = _player.LastSafeGroundPos;
            _player.ResetPhysicsInterpolation3D();
            _player.Velocity = Vector3.Zero;
            _player.ChangeState<PlayerStandState>();
        }
    }
}