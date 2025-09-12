using Godot;

namespace FastDragon
{
    public partial class PlayerFlyInLandState : PlayerState
    {
        public override void OnStateEntered()
        {
            Self.Animator.Play("ParachuteLand", 0.1);

            // Pause the game while flying in.  This way, the fly-in won't
            // affect cycles.
            GetTree().Paused = true;
            Self.ProcessMode = Node.ProcessModeEnum.Always;
        }

        public override void OnStateExited()
        {
            GetTree().Paused = false;
            Self.ProcessMode = Node.ProcessModeEnum.Inherit;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            if (!Self.Animator.IsPlaying())
            {
                Self.Respawn();
                Self.EmitSignal(Player.SignalName.FlyInFinished);
            }
        }
    }
}