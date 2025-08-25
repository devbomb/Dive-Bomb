using System.Linq;
using Godot;

namespace FastDragon
{
    // TODO: Refactor DivableWall to use this, instead of duplicating it.
    public partial class ShatterableWall : BreakableStaticBody3D
    {
        [Export] public GpuParticles3D ShatterPartciles;
        [Export] public float ShatterDuration = 4f / 60;
        [Export] public double HitStopDuration = 0.2;

        private Node3D _meshHolder => GetNode<Node3D>("%MeshHolder");
        private Node3D _meshGrowPoint => GetNode<Node3D>("%MeshGrowPoint");

        private readonly StateMachine _stateMachine = new StateMachine();

        public override void _Ready()
        {
            AddChild(_stateMachine);

            var meshes = this
                .EnumerateDescendantsOfType<MeshInstance3D>()
                .ToArray();

            foreach (var mesh in meshes)
            {
                var globalPos = mesh.GlobalTransform;

                mesh.GetParent().RemoveChild(mesh);
                _meshHolder.AddChild(mesh);

                mesh.GlobalTransform = globalPos;
            }

            Broken += StartShattering;

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            _stateMachine.ChangeState<Solid>();
        }

        private void StartShattering()
        {
            _stateMachine.ChangeState<Shattering>();
        }

        private void SetCollisionEnabled(bool enabled)
        {
            var shapes = this.EnumerateDescendantsOfType<CollisionShape3D>();
            foreach (var shape in shapes)
                shape.Disabled = !enabled;
        }

        private void SetTransparency(float transparency)
        {
            foreach (var mesh in _meshHolder.EnumerateDescendantsOfType<MeshInstance3D>())
                mesh.Transparency = transparency;
        }

        private class Solid : State<ShatterableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(true);
                Self._meshHolder.Visible = true;
                Self.SetTransparency(0);
            }
        }

        private class Shattering : State<ShatterableWall>
        {
            private const float EndScale = 4f;

            private float _timer;
            private float _modelTimer;

            public override void OnStateEntered()
            {
                HitStopManager.Instance.StopFor(Self.HitStopDuration);

                Self.SetCollisionEnabled(false);
                _timer = 0;
                _modelTimer = 0;

                SetPivotPointToPlayer();

                if (Self.ShatterPartciles != null)
                    SpawnParticles();
            }

            public override void OnStateExited()
            {
                Self._meshGrowPoint.Scale = Vector3.One;
                Self._meshGrowPoint.Position = Vector3.Zero;
                Self._meshHolder.Position = Vector3.Zero;

                if (Self.ShatterPartciles != null)
                    Self.ShatterPartciles.Emitting = false;
            }

            public override void _Process(double deltaD)
            {
                _modelTimer += (float)deltaD;

                float t = _modelTimer / Self.ShatterDuration;
                Self._meshGrowPoint.Scale = Vector3.One.Lerp(Vector3.One * EndScale, t * t);
                Self.SetTransparency(Mathf.Min(t * 2, 1));
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                if (_timer >= Self.ShatterDuration)
                {
                    ChangeState<BrokenState>();
                }
            }

            private void SetPivotPointToPlayer()
            {
                var holderPos = Self._meshHolder.GlobalTransform;

                var player = GetTree().FindNode<Player>();
                Self._meshGrowPoint.GlobalPosition = player.GlobalPosition;
                Self._meshHolder.GlobalTransform = holderPos;
            }

            private void SpawnParticles()
            {
                Self.ShatterPartciles.Restart();
                Self.ShatterPartciles.Emitting = true;

                // Walls can be large.  If we always spawn the particles at the
                // origin point, it could be far away from where the player actually
                // hit, which would look weird.  Therefore, let's spawn the
                // particles at the player's position.
                var player = GetTree().FindNode<Player>();
                Self.ShatterPartciles.GlobalPosition = player.GlobalPosition;
                Self.ShatterPartciles.GlobalPosition += Vector3.Up;
            }
        }

        private class BrokenState : State<ShatterableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(false);
                Self._meshHolder.Visible = false;
            }
        }
    }
}