using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class LevelExitCanonRequirementsDisplay : Node3D
    {
        private Node3D _fairies => GetNode<Node3D>("%Fairies");

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            SignalBus.Instance.ExitReached += OnExitReached;
            Reset();

            Callable.From(() =>
            {
                var summary = this.GetLevel()?.GetSummary();
                _fairies.Visible = (summary?.TotalFairiesInLevel ?? 0) > 0;
            }).CallDeferred();
        }

        private void Reset()
        {
            Visible = true;
        }

        private void OnExitReached()
        {
            Visible = false;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            var lookPoint = GetTree().Root.GetCamera3D().GlobalPosition;
            lookPoint.Y = GlobalPosition.Y;

            if (GlobalPosition.DistanceTo(lookPoint) > 0.1f)
                LookAt(lookPoint);
        }
    }
}