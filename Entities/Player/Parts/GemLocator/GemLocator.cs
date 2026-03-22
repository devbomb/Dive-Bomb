using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class GemLocator : Node3D
    {
        private const float GrowTime = 0.1f;
        private const float ShrinkTime = 0.1f;

        [ExportGroup("Internal")]
        [Export] public Node3D GrowPoint;
        [Export] public Node3D Swivel;

        public override void _PhysicsProcess(double delta)
        {
            var nearestGem = GetTree()
                .GetNodesInGroup("Gems")
                .OfType<Gem>()
                .Where(g => !g.IsCollected && !g.IsHomingIn)
                .OrderBy(g => g.GlobalPosition.DistanceSquaredTo(GlobalPosition))
                .FirstOrDefault();

            Swivel.Transform = Swivel.Transform.InterpolateWith(
                TargetSwivelTransformLocal(nearestGem),
                0.1f
            );

            bool shouldShow = nearestGem != null && InputService.LocateGemsHeld;
            GrowPoint.Scale = shouldShow
                ? GrowPoint.Scale.MoveToward(Vector3.One, (float)delta / GrowTime)
                : GrowPoint.Scale.MoveToward(Vector3.Zero, (float)delta / ShrinkTime);
        }

        private Transform3D TargetSwivelTransformLocal(Gem nearestGem)
        {
            if (nearestGem == null)
                return Transform3D.Identity;

            return Transform3D
                .Identity
                .LookingAt(ToLocal(nearestGem.GlobalPosition));
        }
    }
}