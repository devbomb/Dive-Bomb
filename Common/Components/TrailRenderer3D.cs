using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TrailRenderer3D : Node3D
    {
        [Export] public int MaxSegments = 60;
        [Export] public float MaxLength = 1;
        [Export] public float StartRadius = 0.1f;
        [Export] public float EndRadius = 0f;
        [Export] public Material Material;

        [Export] public bool Active;

        private readonly Queue<Vector3> _points = new Queue<Vector3>();
        private readonly List<TrailPointConnector> _connectorPool = new List<TrailPointConnector>();

        public override void _PhysicsProcess(double deltaD)
        {
            // Add the current position to the buffer, bumping out old values
            // if it's over capacity
            _points.Enqueue(GlobalPosition);

            while (_points.Count > MaxSegments)
                _points.Dequeue();

            // Clear out the existing connectors so we can add them again with
            // recalculated positions.
            // Don't free them, though, because they'll be reused from the pool
            // to avoid allocations.
            while (GetChildCount() > 0)
                RemoveChild(GetChild(0));

            if (!Active)
            {
                _points.Clear();
                return;
            }

            if (!_points.Any())
                return;

            // Create a mesh for each of the points, being careful not to
            // exceed the length limit.
            Vector3 lastPoint = _points.Last();
            float totalDist = 0;
            for (int i = 0; i < _points.Count; i++)
            {
                var point = _points.Reverse().ElementAt(i);

                float dist = lastPoint.DistanceTo(point);
                var dir = lastPoint.DirectionTo(point);
                totalDist += dist;

                var clippedPoint = totalDist < MaxLength
                    ? point
                    : lastPoint + ((totalDist - MaxLength) * dir);

                float startRadius = Mathf.Lerp(StartRadius, EndRadius, ((float)i) / _points.Count);
                float endRadius = Mathf.Lerp(StartRadius, EndRadius, ((float)(i + 1)) / _points.Count);

                var connector = GetConnectorFromPool(i);
                AddChild(connector);
                connector.TopLevel = true;
                connector.ConnectPoints(lastPoint, startRadius, clippedPoint, endRadius);

                lastPoint = clippedPoint;

                if (totalDist >= MaxLength)
                    break;
            }
        }

        private TrailPointConnector GetConnectorFromPool(int index)
        {
            while (index >= _connectorPool.Count)
                _connectorPool.Add(new TrailPointConnector(Material));

            return _connectorPool[index];
        }

        private partial class TrailPointConnector : Node3D
        {
            public MeshInstance3D Shaft { get; }
            public MeshInstance3D Dot { get; }

            private readonly Material _material;

            private readonly SphereMesh _dotMesh = new SphereMesh
            {
                RadialSegments = 8,
                Rings = 8
            };
            private readonly CylinderMesh _shaftMesh = new CylinderMesh
            {
                Height = 1,
                RadialSegments = 8,
                Rings = 1
            };

            public TrailPointConnector(Material material)
            {
                _material = material;

                Shaft = new MeshInstance3D();
                Dot = new MeshInstance3D();
                AddChild(Shaft);
                AddChild(Dot);

                Shaft.Mesh = _shaftMesh;
                Shaft.MaterialOverride = _material;
                Shaft.RotationDegrees = new Vector3(90, 0, 0);
                Shaft.Position = new Vector3(0, 0, -0.5f);

                Dot.Mesh = _dotMesh;
                Dot.MaterialOverride = _material;
                Dot.TopLevel = true;
            }

            public void ConnectPoints(
                Vector3 startPoint,
                float startRadius,
                Vector3 endPoint,
                float endRadius
            )
            {
                if (startPoint.IsEqualApprox(endPoint))
                {
                    Shaft.Visible = false;
                    return;
                }

                Shaft.Visible = true;

                _shaftMesh.TopRadius = startRadius;
                _shaftMesh.BottomRadius = endRadius;
                _dotMesh.Radius = startRadius;
                _dotMesh.Height = startRadius * 2;

                Dot.GlobalPosition = startPoint;
                GlobalPosition = startPoint;
                Scale = new Vector3(1, 1, startPoint.DistanceTo(endPoint));

                float componentAlongUp = startPoint.DirectionTo(endPoint).Dot(Vector3.Up);
                bool isParallelToUp = Mathf.IsEqualApprox(Mathf.Abs(componentAlongUp), 1);
                if (!isParallelToUp && !GlobalPosition.IsEqualApprox(endPoint))
                    LookAt(endPoint);
                else
                    GlobalRotationDegrees = new Vector3(-90, 0, 0);
            }
        }
    }
}