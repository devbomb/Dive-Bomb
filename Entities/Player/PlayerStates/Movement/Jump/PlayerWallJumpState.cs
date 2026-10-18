using Godot;

namespace FastDragon
{
    public partial class PlayerWallJumpState : PlayerJumpState
    {
        public override bool CanBoundAfterLanding => true;

        // Disable horizontal controls until the minimum duration has
        // passed, to prevent the player from scaling walls of indefinite
        // height
        public bool AirControlEnabled => _timer > Player.WallJump.DisableStrafeDuration;

        public override float RotSpeedRad => AirControlEnabled
            ? Player.Jump.RotSpeedRad
            : 0;

        public override float StrafeAccel => AirControlEnabled
            ? Player.Jump.StrafeAccel
            : 0;

        private double _timer;

        public override void OnStateEntered()
        {
            Self.ResetPhysicsInterpolation3D();
            base.OnStateEntered();

            Self.FSpeed = Player.WallJump.FSpeed;
            _timer = 0;
        }

        public override void _PhysicsProcess(double delta)
        {
            _timer += delta;
            base._PhysicsProcess(delta);
        }
    }
}