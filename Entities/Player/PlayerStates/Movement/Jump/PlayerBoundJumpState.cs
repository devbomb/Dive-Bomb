using Godot;

namespace FastDragon
{
    public partial class PlayerBoundJumpState : PlayerJumpState
    {
        public override string AnimationName => "BoundJump";

        public override float InitVSpeed => Player.BoundJump.InitVSpeed;
        public override float FullJumpRiseGravity => Player.BoundJump.FullJumpRiseGravity;
        public override float ShortHopGravity => Player.BoundJump.ShortHopGravity;
    }
}