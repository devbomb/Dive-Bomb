using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class ThickBeam : Node3D
    {
        [Signal] public delegate void DamagedPlayerEventHandler();

        [Export] public float Radius = 3;
        [Export] public float MaxLength = 100;
        [Export] public float ExtraLength = 6;
        [Export] public float ProjectorLength = 100;
        [Export] public float ProjectorBorderSize = 0.1f;
        [Export] public bool DamageEnabled = false;

        public Vector3 TargetPos;

        private Area3D _hurtBox => GetNode<Area3D>("%HurtBox");
        private CylinderShape3D _hurtBoxShape => (CylinderShape3D)GetNode<CollisionShape3D>("%HurtBoxShape").Shape;
        private Node3D _model => GetNode<Node3D>("%Model");

        private Decal _intersectionProjector => GetNode<Decal>("%IntersectionProjector");

        public override void _PhysicsProcess(double deltaD)
        {
            AimAtTarget();

            // Damage the player if they're in the hurtbox
            if (DamageEnabled)
            {
                var player = _hurtBox.GetOverlappingBodies()
                    .Where(b => b is Player)
                    .Cast<Player>()
                    .FirstOrDefault();

                if (player?.TryDamage<PlayerDamageFlipState>() ?? false)
                    EmitSignal(SignalName.DamagedPlayer);
            }
        }

        private void AimAtTarget()
        {
            LookAt(TargetPos);

            float length = Mathf.Min(
                GlobalPosition.DistanceTo(TargetPos) + ExtraLength,
                MaxLength
            );

            _hurtBoxShape.Height = length;
            _hurtBoxShape.Radius = Radius;
            _hurtBox.Position = Vector3.Forward * length / 2;

            _model.Scale = new Vector3(Radius, length, Radius);
            _model.Position = Vector3.Forward * length / 2;

            float projectorRadius = Radius + ProjectorBorderSize;
            _intersectionProjector.Position = Vector3.Forward * ProjectorLength / 2;
            _intersectionProjector.Size = new Vector3(
                projectorRadius * 2,
                ProjectorLength,
                projectorRadius * 2
            );
        }
    }
}