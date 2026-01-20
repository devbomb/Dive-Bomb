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
            if (!IsCurrent && !this.IsTimeTrialMode())
                LabelAnimator.Play("Activated");

            SaveFileManager.Current.CurrentLevelVisit.LastCheckpoint = CheckpointName;
            player.Health = Player.MaxHealth;

            if (!this.IsTimeTrialMode())
            {
                SaveFileManager.Instance.RequestAutosave();
            }

            _sparkleBurst.Emitting = true;
            EmitSignal(SignalName.Activated);
            GetTree().FindNode<Player>().Camera.Shake(1, 5, 0.2f);
        }
    }
}