using System;
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
            SetRequirementsVisible(this.GetLevel()?.TimeTrial.IsTimeTrialMode ?? false);
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
            foreach (var m in Enum.GetValues<TimeTrialCategory>())
                _requirementsDisplay.GetNode<Node3D>(m.ToString()).Visible = false;

            var display = _requirementsDisplay.GetNode<Node3D>(TimeTrialCategory.AnyPercent.ToString());
            display.Visible = visible;
        }
    }
}