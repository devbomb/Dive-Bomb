using Godot;

namespace FastDragon
{
    public partial class PlayerDamageFlipState : PlayerState
    {
        public override bool Invincible => true;

        private const float VSpeedBoost = 5;

        public override void OnStateEntered()
        {
            _player.Camera.DisableInput = false;
            _player.Animator.Play("DamageFlip");
            _player.VSpeed += VSpeedBoost;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyGravity(delta);
            DecelerateHSpeedToZero(delta);
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

        public override void OnStateExited()
        {
            // Reset the the animator to avoid the "360 degree wrap around" effect.
            //
            // This animation ends with the player's rotation being 360 degrees,
            // which is NOT technically the same as 0 degrees!
            // When Godot blends this animation with another, it tweens the
            // player's rotation from 360 all the way back down to 0, instead of
            // noticing that the rotation is congruent.
            //
            // Resetting the animator immediately sets the rotation back to 0
            // degrees, thus avoiding the effect at the cost of forgoing
            // animation blending.
            _player.Animator.Play("RESET", 0);
            _player.Animator.Seek(0, true);
        }
    }
}