using Godot;

namespace FastDragon
{
    public partial class PlayerSideFlipState : PlayerJumpState
    {
        public override bool CanBoundAfterLanding => true;

        public override string AnimationName => "SideFlip";
        public override float InitVSpeed => Player.SideFlip.InitVSpeed;
        public override float FullJumpRiseGravity => Player.SideFlip.FullJumpRiseGravity;
        public override float ShortHopGravity => Player.SideFlip.ShortHopGravity;

        public override void OnStateEntered()
        {
            base.OnStateEntered();

            RotateInstantlyTowardLeftStick();
            Self.FSpeed = Player.Walk.Speed;
            Self.ResetPhysicsInterpolation3D();
        }
    }
}