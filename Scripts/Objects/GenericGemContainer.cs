using Godot;

namespace FastDragon
{
    public partial class GenericGemContainer : Node3D, IGemContainer
    {
        [Export] public GemColor GemColor { get; set; } = GemColor.Red;

        [Export] public Node3D Model;
        [Export] public GemSpawner GemSpawner;
        [Export] public CollisionShape3D CollisionShape;
        [Export] public GpuParticles3D Particles;
        [Export] public AudioStreamPlayer3D ShatterSound;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            CollisionShape.Disabled = GemSpawner.IsGemCollected;
            Model.Visible = !GemSpawner.IsGemCollected;
        }

        public void OnBroken()
        {
            CollisionShape.Disabled = true;
            Model.Visible = false;
            Particles.Emitting = true;
            ShatterSound.Play();
            GemSpawner.Reveal();
        }
    }
}