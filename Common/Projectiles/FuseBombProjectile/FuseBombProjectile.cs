using Godot;

namespace FastDragon
{
    public partial class FuseBombProjectile : CharacterBody3D
    {
        [Export] public bool Reusable;
        [Export] public double FuseDurationSeconds = 3;
        [Export] public float ExplosionRadius = 5;

        [ExportCategory("Internal")]
        [Export] public CollisionShape3D BodyShape;
        [Export] public ExplosionProjectile Explosion;
        [Export] public Node3D BombModel;

        private readonly StateMachine _stateMachine = new();

        public FuseBombProjectile()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            Explosion.Radius = ExplosionRadius;
            SignalBus.Instance.LevelReset += OnLevelReset;
            Reset();
        }

        private void OnLevelReset()
        {
            if (!Reusable)
            {
                QueueFree();
                return;
            }

            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Hidden>();
        }

        public void Reveal()
        {
            _stateMachine.ChangeState<Falling>();
        }

        private class Hidden : State<FuseBombProjectile>
        {
            public override void OnStateEntered()
            {
                Self.BombModel.Visible = false;
                Self.BodyShape.Disabled = true;
            }

            public override void OnStateExited()
            {
                Self.BombModel.Visible = true;
                Self.BodyShape.Disabled = false;
            }
        }

        private class Falling : State<FuseBombProjectile>
        {
            private const float UpwardSpeed = 15;
            private const float Gravity = 40;

            public override void OnStateEntered()
            {
                Self.Velocity = Vector3.Up * UpwardSpeed;
            }

            public override void _PhysicsProcess(double delta)
            {
                Self.Velocity += Vector3.Down * Gravity * (float)delta;
                Self.MoveAndSlide();

                if (Self.IsOnFloor())
                    ChangeState<TickingDown>();
            }
        }

        private class TickingDown : State<FuseBombProjectile>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Self.FuseDurationSeconds;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                if (_timer <= 0)
                {
                    Self.Explosion.Explode();
                    ChangeState<Hidden>();
                }
            }
        }
    }
}