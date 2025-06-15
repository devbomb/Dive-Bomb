using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class DivableWall : StaticBody3D, IBreakable
    {
        public bool VulnerableToKick => false;
        public float CameraShakeMagnitude => 0.5f;

        private Node3D _meshHolder => GetNode<Node3D>("%MeshHolder");
        private Node3D _meshGrowPoint => GetNode<Node3D>("%MeshGrowPoint");
        private GpuParticles3D _shatterPartciles => GetNode<GpuParticles3D>("%ExplosionParticles");

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

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            _stateMachine.ChangeState<Solid>();
        }

        public void OnBroken()
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

        private partial class Solid : State<DivableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(true);
                Self._meshHolder.Visible = true;
                Self.SetTransparency(0);
            }
        }

        private partial class Shattering : State<DivableWall>
        {
            private const float Duration = 2f / 60;
            private const float EndScale = 2f;
            private const float HitStopDuration = 0.2f;

            private float _timer;
            private float _modelTimer;

            public override void OnStateEntered()
            {
                HitStopManager.Instance.StopFor(HitStopDuration);

                Self.SetCollisionEnabled(false);
                _timer = 0;
                _modelTimer = 0;

                SetPivotPointToPlayer();
                SpawnParticles();
                Self.GetNode<AudioStreamPlayer>("%ShatterSound").Play();
            }

            public override void OnStateExited()
            {
                Self._shatterPartciles.Emitting = false;
                Self._meshGrowPoint.Scale = Vector3.One;
                Self._meshGrowPoint.Position = Vector3.Zero;
                Self._meshHolder.Position = Vector3.Zero;
            }

            public override void _Process(double deltaD)
            {
                _modelTimer += (float)deltaD;

                float t = _modelTimer / Duration;
                Self._meshGrowPoint.Scale = Vector3.One.Lerp(Vector3.One * EndScale, t * t);
                Self.SetTransparency(t);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                if (_timer >= Duration)
                {
                    ChangeState<Broken>();
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
                Self._shatterPartciles.Restart();
                Self._shatterPartciles.Emitting = true;

                // Walls can be large.  If we always spawn the particles at the
                // origin point, it could be far away from where the player actually
                // hit, which would look weird.  Therefore, let's spawn the
                // particles at the player's position.
                var player = GetTree().FindNode<Player>();
                Self._shatterPartciles.GlobalPosition = player.GlobalPosition;
                Self._shatterPartciles.GlobalPosition += Vector3.Up;
            }
        }

        private partial class Broken : State<DivableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(false);
                Self._meshHolder.Visible = false;
            }
        }
    }
}