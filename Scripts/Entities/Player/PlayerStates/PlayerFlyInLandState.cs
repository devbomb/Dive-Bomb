using Godot;

namespace FastDragon
{
    public partial class PlayerFlyInLandState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Animator.Play("ParachuteLand", 0.1);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            if (!_player.Animator.IsPlaying())
                _player.Respawn();
        }
    }
}