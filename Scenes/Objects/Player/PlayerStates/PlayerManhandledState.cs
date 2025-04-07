using Godot;

namespace FastDragon
{
    /// <summary>
    ///  While in this state, the player's movement is being controlled by
    ///  some other object---such as a whirlwind or a portal.
    /// </summary>
    public partial class PlayerManhandledState : PlayerState
    {
        public override bool Invincible => true;
        public override bool DisableCameraInput => true;
        public override bool UseMario64CameraFocus => false;
    }
}