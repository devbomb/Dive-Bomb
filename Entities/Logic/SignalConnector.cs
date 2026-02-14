using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class SignalConnector : Node3D
    {
        [Export] public string target;
        [Export] public string Signal;

        [Export] public string killtarget;
        [Export] public string HandlerFunction;

        public override void _Ready()
        {
            GetTree().CurrentScene.Ready += () =>
            {
                var nodesByTargetName = GetTree().Root
                    .EnumerateDescendants()
                    .Where(n => !string.IsNullOrEmpty(n.Get("targetname").AsString()))
                    .ToDictionary(n => n.Get("targetname").AsString());

                Node from = nodesByTargetName[target];
                Node to = nodesByTargetName[killtarget];

                from.Connect(Signal, new Callable(to, HandlerFunction));
            };
        }
    }
}