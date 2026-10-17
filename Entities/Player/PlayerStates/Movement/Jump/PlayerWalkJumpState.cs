using Godot;

namespace FastDragon
{
    public partial class PlayerWalkJumpState : PlayerJumpState
    {
        public override bool CanBoundAfterLanding => true;
    }
}