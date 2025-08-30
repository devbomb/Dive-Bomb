using Godot;

namespace FastDragon
{
    public partial class PlayerReachOutDeathState : PlayerState
    {
        public override bool Invincible => true;

        public override void OnStateEntered()
        {
            Self.Animator.Play("ReachOutDeath", 0);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            ApplyGravity(delta);
            DecelerateHSpeedToZero(delta);
            Self.MoveAndSlide();

            if (!Self.Animator.IsPlaying())
                LevelTransitionManager.Instance.RespawnPlayerAfterDeath();
        }
    }
}