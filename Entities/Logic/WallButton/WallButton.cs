using Godot;
using System;

namespace FastDragon
{
    public partial class WallButton : Node3D
    {
        [Signal] public delegate void PressedEventHandler();

        [Export] public string targetname;

        public void Press()
        {
            EmitSignal(SignalName.Pressed);
        }
    }
}
