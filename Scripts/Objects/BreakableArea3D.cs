using Godot;

namespace FastDragon
{
    public partial class BreakableArea3D : Area3D, IBreakable
    {
        [Signal] public delegate void KickedEventHandler();
        [Signal] public delegate void RolledIntoEventHandler();
        [Signal] public delegate void BrokenEventHandler();

        [Export] public float CameraShakeMagnitude { get; set; } = 0.25f;
        [Export] public float CameraShakeFrequency { get; set; } = 15;
        [Export] public float CameraShakeDuration { get; set; } = 0.5f;

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