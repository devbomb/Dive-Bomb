using Godot;

namespace FastDragon
{
    public partial class PlayerDamageFlipState : PlayerState
    {
        public override bool Invincible => true;
        public override bool PauseDamageCooldownTimer => true;

        private const float VSpeed = 5;
        private bool _startedLandingAnimation;

        public override void OnStateEntered()
        {
            Self.Animator.Play("DamageFlip");
            Self.GetNode<AudioStreamPlayer>("%FlipDamageSound").Play();
            Self.VSpeed = VSpeed;
            Self.FSpeed = 0;
            _startedLandingAnimation = false;

            Self.Camera.Shake(1.1f, 15, 0.5f);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyGravity(delta);
            DecelerateHSpeedToZero(delta);
            Self.MoveAndSlide();

            if (Self.IsOnFloor() && !Self.Animator.IsPlaying())
            {
                if (!_startedLandingAnimation)
                {
                    _startedLandingAnimation = true;
                    Self.Animator.Play("DamageFlip_Land");
                    return;
                }

                int currentHealth = (int)SaveFileManager.Current.PlayerHealth;

                if (currentHealth <= 0)
                {
                    Self.ChangeState<PlayerReachOutDeathState>();
                }
                else
                {
                    Self.ChangeState<PlayerWalkState>();
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
            Self.Animator.Play("RESET", 0);
            Self.Animator.Seek(0, true);
        }

        private void DecelerateHSpeedToZero(float delta)
        {
            var v = Self.Velocity.Flattened();
            v = v.MoveToward(Vector3.Zero, Player.Walk.Decel * delta);
            v.Y = Self.Velocity.Y;

            Self.Velocity = v;
        }
    }
}