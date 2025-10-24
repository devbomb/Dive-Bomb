using Godot;

namespace FastDragon
{
    public partial class HandCannonBall : StaticBody3D
    {
        public float Speed = 10;
        public float Lifetime = 3;

        private Node3D _model => GetNode<Node3D>("%Model");
        private GpuParticles3D _trailParticles => GetNode<GpuParticles3D>("%TrailParticles");
        private GpuParticles3D _explosionParticles => GetNode<GpuParticles3D>("%ExplosionParticles");
        private CollisionShape3D _collisionShape => GetNode<CollisionShape3D>("%CollisionShape3D");

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
                Self._model.Visible = false;
                Self._collisionShape.Disabled = true;
                Self._explosionParticles.Emitting = true;
                Self._trailParticles.Emitting = false;

                _timer = Self._explosionParticles.Lifetime * 2;
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