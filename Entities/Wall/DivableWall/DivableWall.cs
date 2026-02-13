using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class DivableWall : StaticBody3D, IBreakable
    {
        private static readonly PackedScene FxScene =
            ResourceLoader.Load<PackedScene>("res://Entities/Wall/DivableWall/DivableWallFX.tscn");

        public bool VulnerableToKick => false;
        public float CameraShakeMagnitude => 0.5f;

        private readonly Node3D _meshGrowPoint = new();
        private readonly Node3D _meshHolder = new();
        private readonly DivableWallFX _fx = FxScene.Instantiate<DivableWallFX>();

        private readonly StateMachine _stateMachine = new StateMachine();


        public override void _Ready()
        {
            AddChild(_stateMachine);
            AddChild(_fx);

            AddChild(_meshGrowPoint);
            _meshGrowPoint.AddChild(_meshHolder);

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

        private class Solid : State<DivableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(true);
                Self._meshHolder.Visible = true;
                Self.SetTransparency(0);
            }
        }

        private class Shattering : State<DivableWall>
        {
            private const float Duration = 4f / 60;
            private const float EndScale = 4f;
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
                Self._fx.Play();
            }

            public override void OnStateExited()
            {
                Self._fx.Stop();
                Self._meshGrowPoint.Scale = Vector3.One;
                Self._meshGrowPoint.Position = Vector3.Zero;
                Self._meshHolder.Position = Vector3.Zero;
            }

            public override void _Process(double deltaD)
            {
                _modelTimer += (float)deltaD;

                float t = _modelTimer / Duration;
                Self._meshGrowPoint.Scale = Vector3.One.Lerp(Vector3.One * EndScale, t * t);
                Self.SetTransparency(Mathf.Min(t * 2, 1));
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
        }

        private class Broken : State<DivableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(false);
                Self._meshHolder.Visible = false;
            }
        }
    }
}