using Godot;

namespace FastDragon
{
    public partial class BossReturnHomePlatform : Node3D
    {
        [Export] public float ExitHeight = 10;

        private Node3D _model => GetNode<Node3D>("%Model");
        private Node3D _hiddenPoint => GetNode<Node3D>("%HiddenPoint");

        private ReturnHomeVortex _vortex => GetNode<ReturnHomeVortex>("%Vortex");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(VortexState));

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            AddChild(_stateMachine);
            _vortex.ExitHeight = ExitHeight;

            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<HiddenState>();
        }

        public void Reveal()
        {
            _stateMachine.ChangeState<RevealingState>();
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
            protected BossReturnHomePlatform Self => _stateMachine.GetParent<BossReturnHomePlatform>();
        }

        private partial class HiddenState : VortexState
        {
            public override void OnStateEntered()
            {
                Self._model.Position = Self._hiddenPoint.Position;
                Self._model.ResetPhysicsInterpolation3D();

                Self.SetParticlesEmitting(false);
            }

            public override void OnStateExited()
            {
                Self.SetParticlesEmitting(true);
            }
        }

        private partial class RevealingState : VortexState
        {
            private const float Duration = 3;
            private float _timer;

            public override void OnStateEntered()
            {
                Self._model.Position = Self._hiddenPoint.Position;
                Self._model.ResetPhysicsInterpolation3D();
                _timer = 0;

                Self.SetParticlesEmitting(false);
            }

            public override void OnStateExited()
            {
                Self.SetParticlesEmitting(true);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                Self._model.Position = Self._hiddenPoint.Position.Lerp(
                    Vector3.Zero,
                    _timer / Duration
                );

                if (_timer >= Duration)
                {
                    Self._model.Position = Vector3.Zero;
                    Self._model.ResetPhysicsInterpolation3D();
                    ChangeState<ReadyState>();
                }
            }
        }

        private partial class ReadyState : VortexState
        {
            public override void OnStateEntered()
            {
                Self._model.Position = Vector3.Zero;
                Self._model.ResetPhysicsInterpolation3D();
                Self._vortex.IsActive = true;
            }

            public override void OnStateExited()
            {
                Self._vortex.IsActive = false;
            }
        }
    }
}