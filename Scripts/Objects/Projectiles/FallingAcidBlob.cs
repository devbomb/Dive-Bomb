using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class FallingAcidBlob : Node3D
    {
        [Export] public float FallHeight = 20;
        [Export] public float FallDuration = 1f;
        [Export] public float PuddleLingerDuration = 1f;
        [Export] public float PuddleRadius = 2;
        [Export] public float BlobRadius = 1;

        private Node3D _blobModel => GetNode<Node3D>("%BlobModel");
        private Node3D _shadow => GetNode<Node3D>("%Shadow");
        private Node3D _puddleModel => GetNode<Node3D>("%PuddleModel");
        private Area3D _puddleArea => GetNode<Area3D>("%PuddleArea");
        private CollisionShape3D _puddleShape => GetNode<CollisionShape3D>("%PuddleShape");
        private CylinderShape3D _puddleCylinder => (CylinderShape3D)_puddleShape.Shape;

        private float _timer = 0;
        private bool _hitGround = false;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += QueueFree;

            _blobModel.Scale = Vector3.One * BlobRadius;
            _blobModel.Position = Vector3.Up * FallHeight;
            _shadow.Scale = Vector3.One * PuddleRadius;
            _puddleModel.Scale = Vector3.One * PuddleRadius;
            _puddleCylinder.Radius = PuddleRadius;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            _timer += (float)deltaD;

            if (!_hitGround)
            {
                float t = _timer / FallDuration;

                _blobModel.Position = (Vector3.Up * FallHeight).Lerp(Vector3.Zero, t);

                if (_timer >= FallDuration)
                {
                    _timer -= FallDuration;
                    _hitGround = true;
                }
            }
            else
            {
                _blobModel.Visible = false;
                _shadow.Visible = false;
                _puddleModel.Visible = true;

                // Damage the player
                var player = _puddleArea.GetOverlappingBodies()
                    .Where(b => b is Player)
                    .Cast<Player>()
                    .FirstOrDefault();

                if (player?.IsOnFloor() ?? false)
                {
                    if (player.TryDamage<PlayerDamageFlipState>())
                        QueueFree();
                }

                // Evaporate after some time
                if (_timer >= PuddleLingerDuration)
                    QueueFree();
            }
        }
    }
}