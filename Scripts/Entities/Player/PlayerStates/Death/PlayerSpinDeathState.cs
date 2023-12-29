using Godot;

namespace FastDragon
{
    public partial class PlayerSpinDeathState : PlayerState
    {
        public override bool Invincible => true;

        public override void OnStateEntered()
        {
            _player.Animator.Play("SpinDeath");
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyGravity(delta);
            DecelerateHSpeedToZero(delta);
            _player.MoveAndSlide();

            if (!_player.Animator.IsPlaying())
                MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        }
    }
}