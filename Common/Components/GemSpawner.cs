using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class GemSpawner : Node3D
    {
        [Export] public bool AlwaysHomeIn;
        public bool IsGemCollected => _gem.IsCollectedInSaveFile;

        private Gem _gem;

        public override void _Ready()
        {
            var parent = GetParent<IGemContainer>();

            _gem = GemFactory.Create(parent.GemColor);
            _gem.StartHidden = true;
            _gem.Name = "Gem";
            AddChild(_gem);

            _gem.TopLevel = true;
        }

        public void Reveal()
        {
            if (!_gem.CanReveal)
                return;

            _gem.Reveal();
            _gem.GlobalPosition = GlobalPosition;
            _gem.ResetPhysicsInterpolation3D();

            if (AlwaysHomeIn)
                _gem.StartHomingIn();
        }
    }
}