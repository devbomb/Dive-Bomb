using Godot;

namespace FastDragon
{
    public partial class LedgeDetector : StaticBody3D
    {
        public const float MaxSlopeAngleDeg = 5f;

        public bool LedgeDetected {get; private set;}

        /// <summary>
        /// Returns the global y position of the ledge the player can grab onto.
        /// If there is no grabbable ledge, it will return a ridiculously high
        /// value.
        /// </summary>
        public float LedgeHeight => LedgeDetected
            ? _lastEdgeCollisionPos.Y
            : float.MaxValue;

        private MeshInstance3D _visualizer => GetNode<MeshInstance3D>("%LedgeCollisionVisualizer");
        private RayCast3D _gapChecker => GetNode<RayCast3D>("%GapChecker");

        private Vector3 _lastEdgeCollisionPos;

        public override void _Process(double delta)
        {
            _visualizer.GlobalPosition = _lastEdgeCollisionPos;
            _visualizer.Transparency = LedgeDetected
                ? 0
                : 0.75f;
        }

        public override void _PhysicsProcess(double delta)
        {
            var ledgeCollision = FindLedgeCollision();
            LedgeDetected = ledgeCollision != null;

            if (LedgeDetected)
                _lastEdgeCollisionPos = ledgeCollision.GetPosition();
        }

        private KinematicCollision3D FindLedgeCollision()
        {
            // HACK: Ignore any bodies that the detector starts inside of.
            //
            // MoveAndCollide() sometimes counts already-overlapping bodies as a
            // collision but not _all_ the time.  When it _does_ detect it as a
            // collision, the distance travelled appears to be random.  This
            // leads to the player sometimes grabbing onto the middle of
            // whichever wall they're rubbing up against.
            //
            // To avoid this, we can just ignore whichever wall(s) it starts
            // inside.  Thankfully, Area3D can RELIABLY tell us what is
            // overlapping.
            var overlapArea = GetNode<Area3D>("%OverlapArea");
            var overlappingStartBodies = overlapArea.GetOverlappingBodies();

            foreach (var body in overlappingStartBodies)
            {
                AddCollisionExceptionWith(body);
            }

            var result = Recursive();

            foreach (var body in overlappingStartBodies)
            {
                RemoveCollisionExceptionWith(body);
            }

            return result;


            KinematicCollision3D Recursive()
            {
                KinematicCollision3D collision = MoveAndCollide(
                    Vector3.Down * Position.Y,
                    testOnly: true
                );

                if (collision == null)
                    return null;

                // Ignore this collision and try again, if it wasn't something
                // we're allowed to grab on to
                bool grabbingAllowed =
                    collision.GetCollider() is StaticBody3D &&
                    collision.GetNormal().AngleTo(Vector3.Up) <= Mathf.DegToRad(MaxSlopeAngleDeg) &&
                    !WallStackedOnTopOfLedge(collision.GetPosition());

                if (!grabbingAllowed)
                {
                    AddCollisionExceptionWith((Node)collision.GetCollider());
                    var result = Recursive();
                    RemoveCollisionExceptionWith((Node)collision.GetCollider());

                    return result;
                }

                return collision;
            }
        }

        /// <summary>
        /// Returns true if there is another wall stacked on top of the ledge
        /// we intend to grab.  This prevents us from grabbing a "ledge" that's
        /// really just a seam between two flush bits of level geometry.
        /// </summary>
        private bool WallStackedOnTopOfLedge(Vector3 edgeCollisionPos)
        {
            var pos = _gapChecker.GlobalPosition;
            pos.Y = edgeCollisionPos.Y + 0.1f;
            _gapChecker.GlobalPosition = pos;
            _gapChecker.ForceUpdateTransform();

            // _gapChecker.TargetPosition = (_gapChecker.GlobalPosition - edgeCollisionPos)
            //     .Flattened();

            // _gapChecker.TargetPosition += _gapChecker.TargetPosition.Normalized() * 0.1f;

            _gapChecker.TargetPosition = Vector3.Forward * (1 + 0.1f);

            _gapChecker.ForceRaycastUpdate();
            return _gapChecker.IsColliding();
        }
    }
}