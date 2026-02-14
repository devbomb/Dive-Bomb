using Godot;

namespace FastDragon
{
    public partial class Checkpoint : Area3D
    {
        [Export] public string CheckpointName;

        [ExportCategory("Internal")]
        [Export] public AnimationPlayer LabelAnimator;

        public bool IsCurrent => SaveFileManager.Current
            .CurrentLevelVisit
            .LastCheckpoint == CheckpointName;

        [Signal] public delegate void ActivatedEventHandler();

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
            if (body is Player player)
            {
                bool needsHealing = player.Health < Player.MaxHealth;

                if (!IsCurrent || needsHealing)
                    Activate(player);
            }
        }

        private void Activate(Player player)
        {
            // Play particle effects to indicate that the checkpoint did
            // something
            _sparkleBurst.Emitting = true;
            EmitSignal(SignalName.Activated);
            GetTree().FindNode<Player>().Camera.Shake(1, 5, 0.2f);

            // Show an announcement if this is a _new_ checkpoint
            if (!IsCurrent && !this.IsTimeTrialMode())
                LabelAnimator.Play("Activated");

            // Heal the player and update their spawn
            player.Health = Player.MaxHealth;
            SaveFileManager.Current.CurrentLevelVisit.LastCheckpoint = CheckpointName;

            // Give other things a chance to react.  Usually by setting a
            // "don't reset me anymore" story flag.
            SignalBus.Instance.EmitCheckpointActivated();

            if (!this.IsTimeTrialMode())
            {
                SaveFileManager.Instance.RequestAutosave();
            }
        }
    }
}