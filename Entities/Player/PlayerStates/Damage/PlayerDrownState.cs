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
            Self.Animator.Play("Drown");
            Self.DrownStartSplashSound.Play();
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            float speed = 1f / DrownDuration;
            Self.GlobalPosition += Vector3.Down * speed * delta;

            _timer -= delta;
            if (_timer <= 0)
            {
                if (SaveFileManager.Current.PlayerHealth > 0)
                    Self.SafeGround.ReturnToLastSafeGround();
                else
                    LevelTransitionManager.Instance.ReloadCheckpoint();
            }
        }
    }
}