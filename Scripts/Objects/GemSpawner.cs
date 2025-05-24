using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class GemSpawner : Node3D
    {
        private Gem _gem;

        public override void _Ready()
        {
            var parent = GetParent<IGemContainer>();

            _gem = GemFactory.Create(parent.GemColor);
            _gem.StartHidden = true;
            _gem.Name = "Gem";
            AddChild(_gem);
        }

        public void Reveal()
        {
            _gem.Reveal();
        }
    }
}