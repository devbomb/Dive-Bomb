using Godot;

namespace FastDragon
{
    public partial class PlayerFlyInLandState : PlayerState
    {
        public override void OnStateEntered()
        {
            _player.Animator.Play("ParachuteLand", 0.1);

            // Pause the game while flying in.  This way, the fly-in won't
            // affect cycles.
            GetTree().Paused = true;
            _player.ProcessMode = Node.ProcessModeEnum.Always;
        }

        public override void OnStateExited()
        {
            GetTree().Paused = false;
            _player.ProcessMode = Node.ProcessModeEnum.Inherit;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            if (!_player.Animator.IsPlaying())
                _player.Respawn();
        }
    }
}