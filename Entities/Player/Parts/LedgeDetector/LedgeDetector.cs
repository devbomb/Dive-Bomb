using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class LedgeDetector : Node3D
    {
        public const float MaxSlopeAngleDeg = 5f;

        public Vector3 LastLedgePoint { get; private set; }
        public bool LastLedgePointRequiresSafeClimb { get; private set; }
        public StaticBody3D LastLedge { get; private set; }


        [ExportCategory("Internal")]
        [Export] public Area3D UpCapsule;
        [Export] public Area3D ForwardCapsule;
        [Export] public RayCast3D DownCast;
        [Export] public RayCast3D AntiSuicideChecker;

        [Export] public Node3D Visualizer;

        public bool IsClimbingPathBlocked =>
            UpCapsule.GetOverlappingBodiesResetSafe().Any(b => b is not Player) ||
            ForwardCapsule.GetOverlappingBodiesResetSafe().Any(b => b is not Player);

        public bool LedgeDetected =>
            DownCast.IsColliding() &&
            DownCast.GetCollider() is StaticBody3D &&
            DownCast.GetCollisionNormal().AngleTo(Vector3.Up) <= Mathf.DegToRad(MaxSlopeAngleDeg);

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
                LastLedge = (StaticBody3D)DownCast.GetCollider();
                LastLedgePoint = DownCast.GetCollisionPoint();
                LastLedgePointRequiresSafeClimb = !AntiSuicideChecker.IsColliding();
                Visualizer.GlobalPosition = LastLedgePoint;
            }
        }

        public void ForceUpdate()
        {
            ForceUpdateTransform();
            DownCast.ForceRaycastUpdate();
        }

        /// <summary>
        /// Returns the position the player should be moved to if they were to
        /// hang from the currently-detected ledge.
        ///
        /// Returns null if no ledge is detected.
        /// </summary>
        public Vector3? GetLedgeHangingPosition(Player player)
        {
            if (!LedgeDetected)
                return null;

            // The height should be such that the ledge grab point is at exactly
            // the ledge height.
            var pos = player.GlobalPosition;
            pos.Y = LedgeGlobalY;
            pos.Y -= player.LedgeGrabPoint.Position.Y;

            // Because the player is a sphere, changing their height would
            // probably make them clip into the wall a little bit.
            // Let's move it out.
            //
            // Don't believe me?  Imagine a billiard ball teetering on the edge
            // of a cliff.  If you just move that ball straight down, it would
            // clip into that cliff, wouldn't it?
            var originalPos = player.GlobalPosition;
            player.GlobalPosition = pos;
            player.MoveAndCollide(Vector3.Zero);
            pos = player.GlobalPosition;
            player.GlobalPosition = originalPos;

            return pos;
        }
    }
}