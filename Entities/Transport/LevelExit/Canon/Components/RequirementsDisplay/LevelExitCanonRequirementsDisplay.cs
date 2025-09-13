using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class LevelExitCanonRequirementsDisplay : Node3D
    {
        private Node3D _requirementsDisplay => GetNode<Node3D>("%RequirementsDisplay");

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            SignalBus.Instance.ExitReached += OnExitReached;
            Reset();
        }

        private void Reset()
        {
            SetRequirementsVisible(true);
        }

        private void OnExitReached()
        {
            SetRequirementsVisible(false);
        }

        public override void _PhysicsProcess(double deltaD)
        {
            var lookPoint = GetTree().Root.GetCamera3D().GlobalPosition;
            lookPoint.Y = GlobalPosition.Y;

            if (GlobalPosition.DistanceTo(lookPoint) > 0.1f)
                LookAt(lookPoint);
        }

        private void SetRequirementsVisible(bool visible)
        {
            foreach (var display in _requirementsDisplay.EnumerateChildren().Cast<Node3D>())
                display.Visible = visible;
        }
    }
}