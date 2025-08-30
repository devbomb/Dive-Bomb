using Godot;

namespace FastDragon
{
    public partial class ReturnHomeVortex : Node3D
    {
        private const float AscendDuration = 3.75f;
        private const float RotSpeedDeg = 180;

        [Export] public float ExitHeight = 15;
        [Export] public bool IsActive;

        private Node3D _spinny => GetNode<Node3D>("%Spinny");
        private Area3D _trigger => GetNode<Area3D>("%Trigger");
        private Player _ensnaredPlayer = null;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += OnLevelReset;
        }

        private void OnLevelReset()
        {
            _ensnaredPlayer = null;
        }

        public override void _Process(double deltaD)
        {
            SetParticlesEmitting(IsActive);

            var rotDeg = _spinny.RotationDegrees;
            rotDeg.Y += (360 / 5) * (float)deltaD;
            _spinny.RotationDegrees = rotDeg;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            if (IsActive && _ensnaredPlayer == null)
                TryEnsarePlayer();

            if (_ensnaredPlayer != null)
                SpinPlayer(delta);
        }

        private void TryEnsarePlayer()
        {
            var bodies = _trigger.GetOverlappingBodiesResetSafe();
            foreach (var body in bodies)
            {
                if (body is Player p)
                    OnPlayerEnteredTrigger(p);
            }
        }

        private void OnPlayerEnteredTrigger(Player p)
        {
            if (p.CurrentState is PlayerManhandledState)
                return;

            _ensnaredPlayer = p;
            p.ChangeState<PlayerManhandledState>();
            p.Animator.Play("Glide");

            GetTree().FindNode<PlayerCamera>().StartFollowing(1);

            SignalBus.Instance.EmitExitReached();
        }

        private void SpinPlayer(float delta)
        {
            // Spin the player and move them up
            float speed = ExitHeight / AscendDuration;
            _ensnaredPlayer.GlobalPosition += Vector3.Up * speed * delta;
            _ensnaredPlayer.GlobalRotationDegrees += Vector3.Up * RotSpeedDeg * delta;

            // Rotate the camera underneath the player
            _ensnaredPlayer.Camera.OrbitPitchRad = AngleMath.DecayToward(
                _ensnaredPlayer.Camera.OrbitPitchRad,
                Mathf.DegToRad(89.999f),
                2,
                delta
            );

            bool playerReachedExitHeight = _ensnaredPlayer.GlobalPosition.Y >= GlobalPosition.Y + ExitHeight;
            bool isTimeTrialMode = GetTree().FindNode<TimeTrialManager>()?.IsTimeTrialMode ?? false;
            if (playerReachedExitHeight && !isTimeTrialMode)
            {
                LevelTransitionManager.Instance.ExitLevel();
            }
        }

        private void SetParticlesEmitting(bool emitting)
        {
            foreach (var particles in this.EnumerateDescendantsOfType<GpuParticles3D>())
            {
                particles.Emitting = emitting;
            }
        }
    }
}