using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class LedgeDetector : Node3D
    {
        public const float MaxSlopeAngleDeg = 5f;

        [ExportCategory("Internal")]
        [Export] public RayCast3D DownCast;
        [Export] public RayCast3D AntiSuicideChecker;

        [Export] public Node3D Visualizer;

        private Player _player;

        public override void _Ready()
        {
            _player = this.FirstAncestor<Player>();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (Visualizer.Visible)
            {
                var ledge = DetectLedge();
                if (ledge.HasValue)
                {
                    Visualizer.GlobalPosition = ledge.Value.LedgePoint;
                }
            }
        }

        public void ForceUpdate()
        {
            ForceUpdateTransform();
            DownCast.ForceRaycastUpdate();
        }

        public DetectedLedge? DetectLedge()
        {
            bool ledgeDetected =
                DownCast.IsColliding() &&
                DownCast.GetCollider() is StaticBody3D &&
                DownCast.GetCollisionNormal().AngleTo(Vector3.Up) <= Mathf.DegToRad(MaxSlopeAngleDeg);

            if (!ledgeDetected)
                return null;

            var hangingPos = GetLedgeHangingPosition();

            var result = new DetectedLedge
            {
                LedgeBody = (StaticBody3D)DownCast.GetCollider(),
                LedgePoint = DownCast.GetCollisionPoint(),
                HangingPosition = hangingPos,
                LedgePlatformVelocity = GetLedgeVelocity(),

                RequiresSafeClimb = !AntiSuicideChecker.IsColliding(),

                IsClimbingPathBlocked =
                    IsClimbingPathBlockedUpwards() ||
                    IsClimbingPathBlockedForwards(),

                IsHangingPosBlocked = _player.TestMove(
                    _player.GlobalTransform,
                    hangingPos - _player.GlobalPosition
                ),
            };

            return result;

            Vector3 GetLedgeHangingPosition()
            {
                // The height should be such that the ledge grab point is at exactly
                // the ledge height.
                var pos = _player.GlobalPosition;
                pos.Y = DownCast.GetCollisionPoint().Y;
                pos.Y -= _player.LedgeGrabPoint.Position.Y;

                // Because the player is a sphere, changing their height would
                // probably make them clip into the wall a little bit.
                // Let's move it out.
                //
                // Don't believe me?  Imagine a billiard ball teetering on the edge
                // of a cliff.  If you just move that ball straight down, it would
                // clip into that cliff, wouldn't it?
                var originalPos = _player.GlobalPosition;
                _player.GlobalPosition = pos;
                _player.MoveAndCollide(Vector3.Zero);
                pos = _player.GlobalPosition;
                _player.GlobalPosition = originalPos;

                return pos;
            }

            Vector3 GetLedgeVelocity()
            {
                var body = (StaticBody3D)DownCast.GetCollider();
                var bs = PhysicsServer3D.BodyGetDirectState(body.GetRid());
                return bs.GetVelocityAtLocalPosition(body.ToLocal(hangingPos));
            }

            bool IsClimbingPathBlockedUpwards()
            {
                Vector3 start = hangingPos;
                Vector3 end = hangingPos;
                end.Y = DownCast.GetCollisionPoint().Y;
                end.Y += _player.SafeMargin;

                return _player.TestMove(
                    Transform3D.Identity.WithOrigin(start),
                    end - start
                );
            }

            bool IsClimbingPathBlockedForwards()
            {
                Vector3 end = DownCast.GetCollisionPoint();
                end.Y += _player.SafeMargin;

                Vector3 start = hangingPos;
                start.Y = end.Y;

                return _player.TestMove(
                    Transform3D.Identity.WithOrigin(start),
                    start.DirectionTo(end) * 0.6f
                );
            }
        }

        public struct DetectedLedge
        {
            /// <summary>
            /// A point on the surface of the ledge.
            /// This is about 1 meter away from the edge.
            /// </summary>
            public Vector3 LedgePoint;

            /// <summary>
            /// The position the player should be moved to if they were to
            /// grab a ledge right now
            /// </summary>
            public Vector3 HangingPosition;

            /// <summary>
            /// The body of the ledge that was detected
            /// </summary>
            public StaticBody3D LedgeBody;

            /// <summary>
            /// The ledge's velocity at the point the player is hanging from.
            /// Used when hanging from moving platforms
            /// </summary>
            public Vector3 LedgePlatformVelocity;

            /// <summary>
            /// True if the climbing path is not blocked, but the player would
            /// need to do a "safe climb" to avoid flinging themselves into the
            /// void.
            /// </summary>
            public bool RequiresSafeClimb;

            /// <summary>
            /// Whether or not the player would be able to climb up this ledge
            /// if they were to hang on it
            /// </summary>
            public bool IsClimbingPathBlocked;

            /// <summary>
            /// Whether or not the player can be safely moved to
            /// <see cref="HangingPosition"/> without colliding with something
            /// </summary>
            public bool IsHangingPosBlocked;
        }
    }
}