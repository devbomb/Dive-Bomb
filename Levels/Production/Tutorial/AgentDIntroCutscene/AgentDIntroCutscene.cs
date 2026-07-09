using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class AgentDIntroCutscene : Node
    {
        [Export] public AnimationPlayer AnimationPlayer;
        [Export] public Camera3D CutsceneCamera;

        public bool IsPlaying => _stateMachine.CurrentState is not Finished;

        private readonly StateMachine _stateMachine = new();

        private Transform3D _restoreCameraInitPos;

        public AgentDIntroCutscene()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Finished>();
        }

        public void Play()
        {
            _stateMachine.ChangeState<Playing>();
        }

        public void Skip()
        {
            _stateMachine.ChangeState<Finished>();
        }

        private class Finished : State<AgentDIntroCutscene> {}

        private class Playing : State<AgentDIntroCutscene>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                Self.CutsceneCamera.MakeCurrent();
                Self.AnimationPlayer.Play("Play");
                _timer = Self.AnimationPlayer.CurrentAnimationLength;

                var player = GetTree().FindNode<Player>();
                player.ChangeState<PlayerManhandledState>();
            }

            public override void OnStateExited()
            {
                Self.AnimationPlayer.Play("RESET");

                var player = GetTree().FindNode<Player>();
                player.ChangeState<PlayerWalkState>();
                player.Camera.MakeCurrent();
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                {
                    // HACK: We need to store the initial pos BEFORE changing
                    // states because the "RESET" animation is about to move
                    // the camera.
                    Self._restoreCameraInitPos = Self.CutsceneCamera.GlobalTransform;
                    ChangeState<RestoringCamera>();
                }
            }
        }

        private class RestoringCamera : State<AgentDIntroCutscene>
        {
            private const double MoveDuration = 0.5;
            private const double PauseDuration = 0.5;

            private double _timer;
            private Player _player;
            private Transform3D _targetPos;

            public override void OnStateEntered()
            {
                _player = GetTree().FindNode<Player>();
                _targetPos = _player.Camera.GlobalTransform;
                _timer = 0;

                _player.ChangeState<PlayerManhandledState>();
                _player.Camera.StartManhandling(Self._restoreCameraInitPos);
                _player.Camera.ResetPhysicsInterpolation3D();
                UpdatePos();

                _player.Animator.Play("DamageFlip_Land");
            }

            public override void OnStateExited()
            {
                _player.Camera.MakeCurrent();
                _player.Camera.StartFollowing(0.1f);
                _player.ChangeState<PlayerWalkState>();
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                if (_timer >= MoveDuration + PauseDuration)
                {
                    ChangeState<Finished>();
                    return;
                }

                UpdatePos();
            }

            private void UpdatePos()
            {
                float t = (float)(_timer / MoveDuration);
                t = Mathf.Min(t, 1.0f);

                _player.Camera.ManhandledPosition = Self
                    ._restoreCameraInitPos
                    .InterpolateWith(_targetPos, t);
            }
        }
    }
}