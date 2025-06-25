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
            Self.Animator.Play("Drown");
            Self.GetNode<AudioStreamPlayer>("%DrownStartSplashSound").Play();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            float speed = 1f / DrownDuration;
            Self.GlobalPosition += Vector3.Down * speed * delta;

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
            Self.GlobalTransform = Self.LastSafeGroundPos;
            Self.ResetPhysicsInterpolation3D();
            Self.Velocity = Vector3.Zero;
            Self.ChangeState<PlayerStandState>();
        }
    }
}