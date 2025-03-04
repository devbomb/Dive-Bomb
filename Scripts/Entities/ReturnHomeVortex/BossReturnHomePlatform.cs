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
            protected BossReturnHomePlatform _self => _stateMachine.GetParent<BossReturnHomePlatform>();
        }

        private partial class HiddenState : VortexState
        {
            public override void OnStateEntered()
            {
                _self._model.Position = _self._hiddenPoint.Position;
                _self._model.ResetPhysicsInterpolation();

                _self.SetParticlesEmitting(false);
            }

            public override void OnStateExited()
            {
                _self.SetParticlesEmitting(true);
            }
        }

        private partial class RevealingState : VortexState
        {
            private const float Duration = 3;
            private float _timer;

            public override void OnStateEntered()
            {
                _self._model.Position = _self._hiddenPoint.Position;
                _self._model.ResetPhysicsInterpolation();
                _timer = 0;

                _self.SetParticlesEmitting(false);
            }

            public override void OnStateExited()
            {
                _self.SetParticlesEmitting(true);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                _self._model.Position = _self._hiddenPoint.Position.Lerp(
                    Vector3.Zero,
                    _timer / Duration
                );

                if (_timer >= Duration)
                {
                    _self._model.Position = Vector3.Zero;
                    _self._model.ResetPhysicsInterpolation();
                    ChangeState<ReadyState>();
                }
            }
        }

        private partial class ReadyState : VortexState
        {
            public override void OnStateEntered()
            {
                _self._model.Position = Vector3.Zero;
                _self._model.ResetPhysicsInterpolation();
                _self._vortex.IsActive = true;
            }

            public override void OnStateExited()
            {
                _self._vortex.IsActive = false;
            }
        }
    }
}