using Godot;

namespace FastDragon
{
    public partial class Checkpoint : Area3D
    {
        [Export] public string CheckpointName;

        public bool IsCurrent => SaveFile.Current.CurrentCheckpoint == CheckpointName;

        private SimpleParticles _sparkleRing => GetNode<SimpleParticles>("%SparkleRing");
        private SimpleParticles _sparkleBurst => GetNode<SimpleParticles>("%SparkleBurst");

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;

            if (CheckpointName == null)
                throw new System.Exception("CheckpointName cannot be null");
        }

        public override void _Process(double deltaD)
        {
            _sparkleRing.Emitting = IsCurrent;
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !IsCurrent)
            {
                SaveFile.Current.CurrentCheckpoint = CheckpointName;
                _sparkleBurst.Emitting = true;
                player.Camera.Shake(1, 5, 0.2f);
            }
        }
    }
}