using System.Security.Cryptography.X509Certificates;
using Godot;

namespace FastDragon
{
    public partial class Checkpoint : Area3D
    {
        [Export] public string CheckpointName;

        /// <summary>
        /// If true, the player will always spawn here when the level loads
        /// and on death.  Use this to test parts of a level without needing
        /// to physically move the player's starting point.
        /// </summary>
        [Export] public bool DebugSpawnHere;

        public bool IsCurrent => SaveFile.Current.CurrentCheckpoint == CheckpointName;

        [Signal] public delegate void ActivatedEventHandler();

        private SimpleParticles _sparkleRing => GetNode<SimpleParticles>("%SparkleRing");
        private SimpleParticles _sparkleBurst => GetNode<SimpleParticles>("%SparkleBurst");

        public override void _Ready()
        {
            BodyEntered += OnBodyEntered;

            if (CheckpointName == null)
                throw new System.Exception("CheckpointName cannot be null");

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            Visible = !IsTimeTrialMode();
        }

        public override void _Process(double deltaD)
        {
            _sparkleRing.Emitting = IsCurrent;
        }

        private void OnBodyEntered(Node3D body)
        {
            if (body is Player player && !IsCurrent && !IsTimeTrialMode())
            {
                SaveFile.Current.CurrentCheckpoint = CheckpointName;
                _sparkleBurst.Emitting = true;
                EmitSignal(SignalName.Activated);
                player.Camera.Shake(1, 5, 0.2f);
            }
        }

        private bool IsTimeTrialMode() => this.GetLevel()?.TimeTrial.IsTimeTrialMode ?? false;
    }
}