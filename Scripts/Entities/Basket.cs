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
            _gem.CurrentState = Gem.State.Hidden;
            _gem.Name = "Gem";
            AddChild(_gem);

            SignalBus.Instance.LevelReset += Reset;
        }

        public void Reset()
        {
            bool gemCollected = SaveFile.Current.IsGemCollected(_gem.GetPath());

            _collisionShape.Disabled = gemCollected;
            _model.Visible = !gemCollected;
        }

        public void OnCharged()
        {
            _collisionShape.Disabled = true;
            _model.Visible = false;
            _gem.StartHomingIn();
        }

        public void OnFlamed()
        {
            _collisionShape.Disabled = true;
            _model.Visible = false;
            _gem.Reveal();
        }
    }
}
