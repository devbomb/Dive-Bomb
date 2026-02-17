using System;
using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class BonkableStaticBody3D : StaticBody3D, IBonkable
    {
        [Signal] public delegate void BonkedEventHandler();

        public void OnBonked()
        {
            EmitSignal(SignalName.Bonked);
        }
    }
}