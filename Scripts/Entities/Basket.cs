using Godot;

namespace FastDragon
{
    public partial class Basket : StaticBody3D, IChargeable, IFlamable
    {
        [Export] public GemColor GemColor = GemColor.Red;

        private CollisionShape3D _collisionShape => GetNode<CollisionShape3D>("%CollisionShape");
        private Node3D _model => GetNode<Node3D>("%Model");
        private Gem _gem;

        public override void _Ready()
        {
            _gem = GemFactory.Create(GemColor);
            _gem.StartHidden = true;
            _gem.Name = "Gem";
            AddChild(_gem);

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            _collisionShape.Disabled = _gem.IsCollected;
            _model.Visible = !_gem.IsCollected;
        }

        public void OnCharged()
        {
            _collisionShape.Disabled = true;
            _model.Visible = false;
            _gem.Reveal();
        }

        public void OnFlamed()
        {
            _collisionShape.Disabled = true;
            _model.Visible = false;
            _gem.Reveal();
        }
    }
}
