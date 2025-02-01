using Godot;

namespace FastDragon
{
    public partial class ReturnHomeVortex : Node3D
    {
        [Export] public float ExitHeight = 10;
        [Export] public bool StartHidden = false;

        private Node3D _model => GetNode<Node3D>("%Model");
        private Node3D _hiddenPoint => GetNode<Node3D>("%HiddenPoint");

        private Area3D _trigger => GetNode<Area3D>("%Trigger");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(VortexState));

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            AddChild(_stateMachine);

            Reset();
        }

        private void Reset()
        {
            if (StartHidden)
                _stateMachine.ChangeState<HiddenState>();
            else
                _stateMachine.ChangeState<ReadyState>();
        }

        public void Reveal()
        {
            _stateMachine.ChangeState<RevealingState>();
        }

        private void SetTriggerMonitoring(bool enabled)
        {
            _trigger.SetDeferred("monitoring", enabled);
        }

        private void SetParticlesEmitting(bool emitting)
        {
            foreach (var particles in this.EnumerateDescendantsOfType<GpuParticles3D>())
            {
                particles.Emitting = emitting;
            }
        }

        private abstract partial class VortexState : State
        {
            protected ReturnHomeVortex _vortex => _stateMachine.GetParent<ReturnHomeVortex>();
        }

        private partial class HiddenState : VortexState
        {
            public override void OnStateEntered()
            {
                _vortex._model.Position = _vortex._hiddenPoint.Position;
                _vortex._model.ResetPhysicsInterpolation3D();

                _vortex.SetParticlesEmitting(false);
            }

            public override void OnStateExited()
            {
                _vortex.SetParticlesEmitting(true);
            }
        }

        private partial class RevealingState : VortexState
        {
            private const float Duration = 3;
            private float _timer;

            public override void OnStateEntered()
            {
                _vortex._model.Position = _vortex._hiddenPoint.Position;
                _vortex._model.ResetPhysicsInterpolation3D();
                _timer = 0;

                _vortex.SetParticlesEmitting(false);
            }

            public override void OnStateExited()
            {
                _vortex.SetParticlesEmitting(true);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                _vortex._model.Position = _vortex._hiddenPoint.Position.Lerp(
                    Vector3.Zero,
                    _timer / Duration
                );

                if (_timer >= Duration)
                {
                    _vortex._model.Position = Vector3.Zero;
                    _vortex._model.ResetPhysicsInterpolation3D();
                    ChangeState<ReadyState>();
                }
            }
        }

        private partial class ReadyState : VortexState
        {
            public override void OnStateEntered()
            {
                _vortex._model.Position = Vector3.Zero;
                _vortex._model.ResetPhysicsInterpolation3D();

                _vortex.SetTriggerMonitoring(true);
                _vortex._trigger.BodyEntered += OnTriggerBodyEntered;
            }

            public override void OnStateExited()
            {
                _vortex.SetTriggerMonitoring(false);
                _vortex._trigger.BodyEntered -= OnTriggerBodyEntered;
            }

            private void OnTriggerBodyEntered(Node body)
            {
                if (body is Player p && !(p.CurrentState is PlayerManhandledState))
                    ChangeState<ExitingLevelState>();
            }
        }

        private partial class ExitingLevelState : VortexState
        {
            private const float AscendDuration = 3.75f;
            private const float RotSpeedDeg = 180;

            private Player _player;

            public override void OnStateEntered()
            {
                _player = GetTree().FindNode<Player>();
                _player.ChangeState<PlayerManhandledState>();
                _player.Animator.Play("Glide");

                TimeTrialSaveData.Instance.UnlockAnyPercent(SaveFile.Current.CurrentMap);
                GetTree().FindNode<TimeTrialManager>()?.Finish();
                GetTree().FindNode<PlayerCamera>().StopFixingPosition();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                if (_player == null)
                    return;

                // Spin the player and move them up
                float speed = _vortex.ExitHeight / AscendDuration;
                _player.GlobalPosition += Vector3.Up * speed * delta;
                _player.GlobalRotationDegrees += Vector3.Up * RotSpeedDeg * delta;

                // Rotate the camera underneath the player
                _player.Camera.OrbitPitchRad = AngleMath.DecayToward(
                    _player.Camera.OrbitPitchRad,
                    Mathf.DegToRad(89.999f),
                    2,
                    delta
                );

                float h = _vortex.GlobalPosition.Y + _vortex.ExitHeight;
                bool playerReachedExitHeight = _player.GlobalPosition.Y >= h;
                bool isTimeTrialMode = GetTree().FindNode<TimeTrialManager>()?.IsTimeTrialMode ?? false;

                if (playerReachedExitHeight && !isTimeTrialMode)
                {
                    MapTransitionManager.Instance.ExitLevel();
                }
            }
        }
    }
}