using Godot;
using System;
using System.Linq;

namespace FastDragon
{
    public partial class GlassPaneSpawner : Area3D
    {
        private static readonly PackedScene FxScene =
            ResourceLoader.Load<PackedScene>("res://Entities/Hazard/GlassPaneSpawner/GlassPaneSpawnerFX.tscn");

        private static readonly PackedScene PaneScene =
            ResourceLoader.Load<PackedScene>("res://Entities/Hazard/GlassPaneSpawner/SpawnedGlassPane/SpawnedGlassPane.tscn");

        [Export] public double SpawnIntervalSeconds = 1;
        [Export] public double SpawnDurationSeconds = 2;
        [Export] public double PaneLifespanSeconds = 5;

        private readonly StateMachine _stateMachine = new();
        private readonly Node3D _activePanes = new();
        private readonly GlassPaneSpawnerFX _fx = FxScene.Instantiate<GlassPaneSpawnerFX>();

        private MeshInstance3D _meshInstance;
        private CollisionShape3D _collisionShape;
        private double _timer;

        public GlassPaneSpawner()
        {
            AddChild(_stateMachine);
            AddChild(_activePanes);
            AddChild(_fx);
        }

        public override void _Ready()
        {
            _meshInstance = this.EnumerateChildren()
                .OfType<MeshInstance3D>()
                .First();
            _meshInstance.Visible = false;

            _collisionShape = this.EnumerateChildren()
                .OfType<CollisionShape3D>()
                .First();

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Waiting>();
        }

        private void SpawnPane()
        {
            var pane = PaneScene.Instantiate<SpawnedGlassPane>();
            pane.LifespanSeconds = PaneLifespanSeconds;
            pane.Initialize(_meshInstance, _collisionShape.Shape);

            _activePanes.AddChild(pane);
            pane.GlobalTransform = GlobalTransform;
            pane.ResetPhysicsInterpolation3D();
        }

        private class Waiting : State<GlassPaneSpawner>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Self.SpawnIntervalSeconds;
                Self._fx.Visible = false;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                    ChangeState<Spawning>();
            }
        }

        private class Spawning : State<GlassPaneSpawner>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;

                Self._fx.Visible = true;
                Self._fx.Initialize(Self._meshInstance.Mesh, (float)Self.SpawnDurationSeconds);

                // Get the player if they were already standing in the hitbox
                // when it turned on.  (IE: They missed the BodyEntered event)
                if (Self.GetOverlappingBodiesResetSafe().OfType<Player>().Any())
                    TryBurnPlayer();
            }

            public override void SubscribeToSignals()
            {
                Self.BodyEntered += OnBodyEntered;
            }

            public override void UnsubscribeFromSignals()
            {
                Self.BodyEntered -= OnBodyEntered;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                if (_timer >= Self.SpawnDurationSeconds)
                {
                    Self._fx.Visible = false;
                    Self.SpawnPane();
                    ChangeState<Waiting>();
                }
            }

            private void OnBodyEntered(Node3D body)
            {
                if (body is Player)
                    TryBurnPlayer();
            }

            private void TryBurnPlayer()
            {
                if (HasCooled())
                    return;

                var player = Self.GetTree().FindNode<Player>();

                if (player.GlobalPosition.Y < GetFillHeight())
                    player.TryDamage<PlayerBurnVoidOutState>();
            }

            private bool HasCooled()
            {
                var fillAnimation = Self._fx.Animator.GetAnimation("Fill");
                double markerTime = fillAnimation.GetMarkerTime("Cooling");

                return (_timer / Self.SpawnDurationSeconds) > (markerTime / fillAnimation.Length);
            }

            private float GetFillHeight()
            {
                var aabb = Self._meshInstance.GetAabb();
                float min = Self.GlobalPosition.Y - (aabb.Size.Y / 2);
                float max = Self.GlobalPosition.Y + (aabb.Size.Y / 2);

                return Mathf.Lerp(min, max, (float)(_timer / Self.SpawnDurationSeconds));
            }
        }
    }
}
