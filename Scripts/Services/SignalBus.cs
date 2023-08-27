using Godot;

namespace FastDragon
{
    public partial class SignalBus : Node
    {
        public static SignalBus Instance {get; private set;}

        [Signal] public delegate void LevelResetEventHandler();

        public override void _Ready()
        {
            Instance = this;
        }

        public void EmitLevelReset() => EmitSignal(SignalName.LevelReset);
    }
}