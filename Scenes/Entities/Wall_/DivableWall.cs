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

        private readonly StateMachine _stateMachine = new StateMachine(typeof(DivableWallState));

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

        private partial class DivableWallState : State
        {
            protected DivableWall _self => _stateMachine.GetParent<DivableWall>();
        }

        private partial class Solid : DivableWallState
        {
            public override void OnStateEntered()
            {
                _self.SetCollisionEnabled(true);
                _self._meshHolder.Visible = true;
                _self.SetTransparency(0);
            }
        }

        private partial class Shattering : DivableWallState
        {
            private const float Duration = 2f / 60;
            private const float EndScale = 2f;
            private const float HitStopDuration = 0.2f;

            private float _timer;
            private float _modelTimer;

            public override void OnStateEntered()
            {
                HitStopManager.Instance.StopFor(HitStopDuration);

                _self.SetCollisionEnabled(false);
                _timer = 0;
                _modelTimer = 0;

                SetPivotPointToPlayer();
                SpawnParticles();
                _self.GetNode<AudioStreamPlayer>("%ShatterSound").Play();
            }

            public override void OnStateExited()
            {
                _self._shatterPartciles.Emitting = false;
                _self._meshGrowPoint.Scale = Vector3.One;
                _self._meshGrowPoint.Position = Vector3.Zero;
                _self._meshHolder.Position = Vector3.Zero;
            }

            public override void _Process(double deltaD)
            {
                _modelTimer += (float)deltaD;

                float t = _modelTimer / Duration;
                _self._meshGrowPoint.Scale = Vector3.One.Lerp(Vector3.One * EndScale, t * t);
                _self.SetTransparency(t);
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
                var holderPos = _self._meshHolder.GlobalTransform;

                var player = GetTree().FindNode<Player>();
                _self._meshGrowPoint.GlobalPosition = player.GlobalPosition;
                _self._meshHolder.GlobalTransform = holderPos;
            }

            private void SpawnParticles()
            {
                _self._shatterPartciles.Restart();
                _self._shatterPartciles.Emitting = true;

                // Walls can be large.  If we always spawn the particles at the
                // origin point, it could be far away from where the player actually
                // hit, which would look weird.  Therefore, let's spawn the
                // particles at the player's position.
                var player = GetTree().FindNode<Player>();
                _self._shatterPartciles.GlobalPosition = player.GlobalPosition;
                _self._shatterPartciles.GlobalPosition += Vector3.Up;
            }
        }

        private partial class Broken : DivableWallState
        {
            public override void OnStateEntered()
            {
                _self.SetCollisionEnabled(false);
                _self._meshHolder.Visible = false;
            }
        }
    }
}