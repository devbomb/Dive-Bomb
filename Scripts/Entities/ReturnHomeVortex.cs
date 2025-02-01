using Godot;

namespace FastDragon
{
    public partial class ReturnHomeVortex : Node3D
    {
        [Export] public float ExitHeight = 15;

        private readonly StateMachine _stateMachine = new StateMachine(typeof(VortexState));

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            AddChild(_stateMachine);

            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<ReadyState>();
        }

        public void OnTriggerBodyEntered(Node body)
        {
            var state = (VortexState)_stateMachine.CurrentState;
            state.OnTriggerBodyEntered(body);
        }

        private abstract partial class VortexState : State
        {
            protected ReturnHomeVortex _vortex => _stateMachine.GetParent<ReturnHomeVortex>();
            public virtual void OnTriggerBodyEntered(Node body) {}
        }

        private partial class ReadyState : VortexState
        {
            public override void OnTriggerBodyEntered(Node body)
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