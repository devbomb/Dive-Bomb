using Godot;

namespace FastDragon
{
    public partial class PlayerDamageFlipState : PlayerState
    {
        public override bool Invincible => true;

        private const float VSpeedBoost = 5;

        public override void OnStateEntered()
        {
            _player.Animator.Play("DamageFlip");
            _player.VSpeed += VSpeedBoost;
        }

        public override void _PhysicsProcess(double delta)
        {
            ApplyGravity((float)delta);
            _player.MoveAndSlide();

            if (!_player.Animator.IsPlaying())
            {
                int currentHealth = (int)SaveFile.Current.PlayerHealth;

                if (currentHealth <= 0)
                {
                    _player.ChangeState<PlayerSpinDeathState>();
                }
                else
                {
                    _player.ChangeState<PlayerWalkState>();
                }
            }
        }
    }
}