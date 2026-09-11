using Godot;

namespace FastDragon
{
    public partial class PlayerFlyInLandState : PlayerState
    {
        public override bool Invincible => true;
        public override bool DisableCameraInput => true;

        public override void OnStateEntered()
        {
            Self.Animator.Play("ParachuteLand", 0.1);

            // Pause the game while flying in.  This way, the fly-in won't
            // affect cycles.
            GetTree().Paused = true;
            Self.ProcessMode = Node.ProcessModeEnum.Always;

            // Start tweening the camera into place
            double animLength = Self.Animator.GetAnimation("ParachuteLand").Length;
            var endPos = Self.Camera.PositionFromOrbitCoords(Self.Camera.RecenteredOrbitCoords());
            Self.Camera.StartManhandling(endPos, (float)animLength);
        }

        public override void OnStateExited()
        {
            GetTree().Paused = false;
            Self.ProcessMode = Node.ProcessModeEnum.Inherit;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!Self.Animator.IsPlaying())
            {
                Self.Reset();
                Self.EmitSignal(Player.SignalName.FlyInFinished);
            }
        }
    }
}