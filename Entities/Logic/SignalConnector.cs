using System;
using Godot;

namespace FastDragon
{
    public partial class SignalConnector : Node3D
    {
        [Export] public string target;
        [Export] public string Signal;

        [Export] public string killtarget;
        [Export] public string HandlerFunction;

        public NodePath From => target;
        public NodePath To => killtarget;
    }
}