using Godot;

namespace FastDragon
{
    public partial class HandCannonBall : StaticBody3D
    {
        [Export] public float Speed = 10;
        [Export] public float Lifetime = 3;

        [ExportGroup("Internal")]
        [Export] public Node3D Model;
        [Export] public CollisionShape3D CollisionShape;
        [Export] public GpuParticles3D TrailParticles;
        [Export] public GpuParticles3D ExplosionParticles;

        private readonly StateMachine _stateMachine = new();

        public HandCannonBall()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += QueueFree;
            _stateMachine.ChangeState<Flying>();
        }

        private class Flying : State<HandCannonBall>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Self.Lifetime;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                if (_timer <= 0)
                {
                    ChangeState<Exploding>();
                    return;
                }

                var velocity = Self.GlobalForward() * Self.Speed;
                var collision = Self.MoveAndCollide(velocity * (float)delta);

                if (collision != null)
                {
                    if (collision.GetCollider() is Player p)
                        p.TryDamage<PlayerDamageFlipState>();

                    ChangeState<Exploding>();
                }
            }
        }

        private class Exploding : State<HandCannonBall>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                Self.Model.Visible = false;
                Self.CollisionShape.Disabled = true;
                Self.ExplosionParticles.Emitting = true;
                Self.TrailParticles.Emitting = false;

                _timer = Self.ExplosionParticles.Lifetime * 2;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                if (_timer <= 0)
                    Self.QueueFree();
            }
        }
    }
}