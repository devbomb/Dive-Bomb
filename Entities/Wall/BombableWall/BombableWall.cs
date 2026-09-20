using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class BombableWall : StaticBody3D, IDamageable
    {
        private static readonly PackedScene FxScene =
            ResourceLoader.Load<PackedScene>("res://Entities/Wall/BombableWall/BombableWallFX.tscn");

        public bool Kickable => false;
        public bool Rollable => false;
        public bool Explodable => true;

        public float CameraShakeMagnitude => 0.5f;

        private readonly BombableWallFX _fx = FxScene.Instantiate<BombableWallFX>();
        private readonly MeshExploder _meshExploder = new();
        private readonly StateMachine _stateMachine = new();

        private MeshInstance3D _meshInstance;

        public BombableWall()
        {
            AddChild(_stateMachine);
            AddChild(_meshExploder);
            AddChild(_fx);
        }

        public override void _Ready()
        {
            _meshInstance = this.EnumerateChildren()
                .OfType<MeshInstance3D>()
                .First();

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            _stateMachine.ChangeState<Solid>();
        }

        public void OnDamaged()
        {
            _stateMachine.ChangeState<Shattering>();
        }

        private void SetCollisionEnabled(bool enabled)
        {
            var shapes = this.EnumerateDescendantsOfType<CollisionShape3D>();
            foreach (var shape in shapes)
                shape.Disabled = !enabled;
        }

        private class Solid : State<BombableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(true);
                Self._meshInstance.Visible = true;
            }
        }

        private class Shattering : State<BombableWall>
        {
            private const float Duration = 4f / 60;
            private const float EndScale = 4f;
            private const float HitStopDuration = 0.2f;

            private float _timer;

            public override void OnStateEntered()
            {
                HitStopManager.Instance.StopFor(HitStopDuration);

                Self.SetCollisionEnabled(false);
                Self._meshInstance.Visible = false;
                _timer = 0;

                Self._meshExploder.Explode(
                    Self._meshInstance,
                    Self.GlobalPosition,
                    EndScale,
                    Duration
                );

                Self._fx.Play(Self._meshInstance);
            }

            public override void OnStateExited()
            {
                Self._fx.Stop();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                if (_timer >= Duration)
                {
                    ChangeState<Broken>();
                }
            }
        }

        private class Broken : State<BombableWall>
        {
            public override void OnStateEntered()
            {
                Self.SetCollisionEnabled(false);
                Self._meshInstance.Visible = false;
            }
        }
    }
}