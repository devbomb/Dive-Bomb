using Godot;

namespace FastDragon
{
    public partial class Vase : StaticBody3D, IBreakable
    {
        public bool VulnerableToKick => false;

        [Export] public GemColor GemColor = GemColor.Red;

        private CollisionShape3D _collisionShape => GetNode<CollisionShape3D>("%CollisionShape");
        private Node3D _model => GetNode<Node3D>("%Model");
        private GpuParticles3D _particles => GetNode<GpuParticles3D>("%Particles");
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

        public void OnBroken()
        {
            _collisionShape.Disabled = true;
            _model.Visible = false;
            _particles.Emitting = true;
            _gem.Reveal();
        }
    }
}
