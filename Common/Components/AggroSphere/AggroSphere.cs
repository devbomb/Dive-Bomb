using Godot;
using System.Linq;

namespace FastDragon
{
    [Tool]
    public partial class AggroSphere : RayCast3D
    {
        [Export] public float Radius
        {
            get => _radius;
            set
            {
                _radius = value;
                UpdateRadius();
            }
        }
        private float _radius = 20;

        private Area3D _area => GetNode<Area3D>("%Area3D");

        public Player SearchForPlayer(int safetyTimer = 2)
        {
            return _area
                    .GetOverlappingBodiesResetSafe(safetyTimer)
                    .Where(body => body is Player)
                    .Cast<Player>()
                    .FirstOrDefault(HasLineOfSightTo);
        }

        public override void _Ready()
        {
            UpdateRadius();
        }

        private void UpdateRadius()
        {
            _area.Scale = Vector3.One * _radius;
        }

        private bool HasLineOfSightTo(Player player)
        {
            var localPlayerPos = ToLocal(player.GlobalPosition);
            TargetPosition = localPlayerPos;
            ForceUpdateTransform();
            ForceRaycastUpdate();

            var collider = GetCollider();
            return (collider == null || collider == player);
        }

        private partial class DebugRing3D : Node3D
        {
            public float Radius
            {
                get => _radius;
                set
                {
                    _radius = value;
                    Outside.Scale = new Vector3(value, 1, value);
                    Inside.Scale = Outside.Scale;
                }
            }
            private float _radius;

            private readonly CylinderMesh MeshOutside = new CylinderMesh
            {
                Height = 0.05f,
                Rings = 1,
                TopRadius = 1,
                BottomRadius = 1,
                CapTop = false,
                CapBottom = false,
            };
            private readonly CylinderMesh MeshInside = new CylinderMesh
            {
                Height = 0.05f,
                Rings = 1,
                TopRadius = 1,
                BottomRadius = 1,
                CapTop = false,
                CapBottom = false,
                FlipFaces = true
            };
            private readonly MeshInstance3D Outside = new MeshInstance3D();
            private readonly MeshInstance3D Inside = new MeshInstance3D();

            public DebugRing3D(Vector3 forward)
            {
                Rotation = forward.ForwardToEulerAnglesRad();
                Outside.Mesh = MeshOutside;
                Inside.Mesh = MeshInside;
            }

            public override void _Ready()
            {
                AddChild(Outside);
                AddChild(Inside);
            }
        }
    }
}