using Godot;

namespace FastDragon
{
    public partial class PlayerFlyInLandState : PlayerState
    {
        public override bool Invincible => true;
        public override bool DisableCameraInput => true;

        private double _timer;

        public override void OnStateEntered()
        {
            _timer = Self.Animator.PlayGetLength("ParachuteLand", 0.1);

            // Pause the game while flying in.  This way, the fly-in won't
            // affect cycles.
            GetTree().Paused = true;
            Self.ProcessMode = Node.ProcessModeEnum.Always;

            // Start tweening the camera into place
            var endPos = Self.Camera.PositionFromOrbitCoords(Self.Camera.RecenteredOrbitCoords());
            Self.Camera.StartManhandling(endPos, (float)_timer);
        }

        public override void OnStateExited()
        {
            GetTree().Paused = false;
            Self.ProcessMode = Node.ProcessModeEnum.Inherit;
        }

        public override void _PhysicsProcess(double delta)
        {
            _timer -= delta;
            if (_timer <= 0)
            {
                Self.Reset();
                Self.EmitSignal(Player.SignalName.FlyInFinished);
            }
        }
    }
}