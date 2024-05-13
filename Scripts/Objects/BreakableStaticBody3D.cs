using Godot;

namespace FastDragon
{
    public partial class BreakableStaticBody3D : StaticBody3D, IBreakable
    {
        [Signal] public delegate void KickedEventHandler();
        [Signal] public delegate void RolledIntoEventHandler();
        [Signal] public delegate void BrokenEventHandler();

        [Export] public bool Rollable { get; set; } = true;
        [Export] public bool Kickable { get; set; } = true;
        [Export] public bool Disabled { get; set; } = false;

        public bool VulnerableToRoll => Rollable && !Disabled;
        public bool VulnerableToKick => Kickable && !Disabled;

        public void OnKicked()
        {
            EmitSignal(SignalName.Kicked);
        }

        public void OnRolledInto()
        {
            EmitSignal(SignalName.RolledInto);
        }

        public void OnBroken()
        {
            EmitSignal(SignalName.Broken);
        }
    }
}