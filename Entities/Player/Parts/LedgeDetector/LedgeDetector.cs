using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class LedgeDetector : Node3D
    {
        public const float MaxSlopeAngleDeg = 5f;

        public Vector3 LastLedgePoint { get; private set; }
        public bool LastLedgePointRequiresSafeClimb { get; private set; }

        [Export] public CharacterBody3D Body;

        [ExportCategory("Internal")]
        [Export] public Area3D UpCapsule;
        [Export] public Area3D ForwardCapsule;
        [Export] public RayCast3D DownCast;
        [Export] public RayCast3D AntiSuicideChecker;

        [Export] public Node3D Visualizer;

        public bool IsBlocked =>
            UpCapsule.GetOverlappingBodiesResetSafe().Any(b => b is not Player) ||
            ForwardCapsule.GetOverlappingBodiesResetSafe().Any(b => b is not Player);

        public bool LedgeDetected => Body.IsOnWallOnly() && DownCast.IsColliding();

        /// <summary>
        /// Returns the global y position of the ledge the player can grab onto.
        /// If there is no grabbable ledge, it will return a ridiculously high
        /// value.
        /// </summary>
        public float LedgeGlobalY => LedgeDetected
            ? DownCast.GetCollisionPoint().Y
            : float.MaxValue;

        public override void _PhysicsProcess(double delta)
        {
            if (DownCast.IsColliding())
            {
                var pos = ForwardCapsule.GlobalPosition;
                pos.Y = DownCast.GetCollisionPoint().Y;
                ForwardCapsule.GlobalPosition = pos;
            }

            if (LedgeDetected)
            {
                LastLedgePoint = DownCast.GetCollisionPoint();
                LastLedgePointRequiresSafeClimb = !AntiSuicideChecker.IsColliding();
                Visualizer.GlobalPosition = LastLedgePoint;
            }
        }
    }
}