using Godot;

namespace FastDragon
{
    public partial class HandCannonBall : StaticBody3D
    {
        public float Speed = 10;

        private Node3D _model => GetNode<Node3D>("%Model");
        private GpuParticles3D _trailParticles => GetNode<GpuParticles3D>("%TrailParticles");
        private GpuParticles3D _explosionParticles => GetNode<GpuParticles3D>("%ExplosionParticles");

        private bool _destroyed = false;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += QueueFree;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            if (_destroyed)
            {
                if (!_explosionParticles.Emitting)
                    QueueFree();

                return;
            }

            var velocity = this.GlobalForward() * Speed;
            var collision = MoveAndCollide(velocity * delta);

            if (collision == null)
                return;

            _destroyed = true;
            _model.Visible = false;
            _explosionParticles.Emitting = true;
            _trailParticles.Emitting = false;

            if (collision.GetCollider() is Player p)
            {
                p.TryDamage<PlayerDamageFlipState>();
            }
        }
    }
}