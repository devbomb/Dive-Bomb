using Godot;

namespace FastDragon
{
    public partial class Vase : StaticBody3D, IChargeable
    {
        private CollisionShape3D _collisionShape => GetNode<CollisionShape3D>("%CollisionShape");

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += () =>
            {
                _collisionShape.Disabled = false;
                Visible = true;
            };
        }

        public void OnCharged()
        {
            _collisionShape.Disabled = true;
            Visible = false;
        }
    }
}