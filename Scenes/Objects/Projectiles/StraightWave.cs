using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class StraightWave : Node3D
    {
        [Signal] public delegate void DamagedPlayerEventHandler();

        [Export] public float Radius = 0.5f;
        [Export] public float StartWidth = 1;
        [Export] public float EndWidth = 10;
        [Export] public float Distance = 10;
        [Export] public float Duration = 1;
        [Export] public float DamageCooldownDuration = 2;

        private Area3D _hitBox => GetNode<Area3D>("%HitBox");
        private CollisionShape3D _collisionShape => GetNode<CollisionShape3D>("%CollisionShape");
        private CapsuleShape3D _capsuleShape => (CapsuleShape3D)_collisionShape.Shape;

        private MeshInstance3D _model => GetNode<MeshInstance3D>("%Model");
        private CapsuleMesh _mesh => (CapsuleMesh)_model.Mesh;

        private float _timer = 0;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += QueueFree;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            // Grow
            _timer += (float)deltaD;
            float t = Mathf.Min(_timer / Duration, 1);

            _mesh.Height = Mathf.Lerp(StartWidth, EndWidth, t);
            _mesh.Radius = Radius;
            _model.Position = Vector3.Zero.Lerp(Vector3.Forward * Distance, t);

            _capsuleShape.Height = _mesh.Height;
            _capsuleShape.Radius = Radius;
            _collisionShape.Position = _model.Position;

            // Damage the player
            var player = _hitBox.GetOverlappingBodies()
                .Where(b => b is Player)
                .Cast<Player>()
                .FirstOrDefault();

            bool dealtDamage = player?.TryDamage<PlayerDamageFlipState>(DamageCooldownDuration) ?? false;
            if (dealtDamage)
            {
                EmitSignal(SignalName.DamagedPlayer);
            }

            // Destroy when done
            if (_timer >= Duration)
            {
                QueueFree();
            }
        }
    }
}