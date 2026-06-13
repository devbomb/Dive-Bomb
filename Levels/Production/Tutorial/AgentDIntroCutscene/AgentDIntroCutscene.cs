using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class AgentDIntroCutscene : Node
    {
        public const string StoryFlag = "AgentDIntroFinished";

        [Export] public AnimationPlayer AnimationPlayer;
        [Export] public Camera3D CutsceneCamera;
        [Export] public BackgroundMusicPlayer BackgroundMusicPlayer;

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
            bool watchedBefore = this.GetLevel()
                .GetProgress()
                .StoryFlags
                .Contains(StoryFlag);

            if (watchedBefore || this.IsTimeTrialMode() || this.PlaySceneFromHereWasUsed())
                _stateMachine.ChangeState<Finished>();
            else
                _stateMachine.ChangeState<Playing>();
        }

        private class Finished : State<AgentDIntroCutscene> {}

        private class Playing : State<AgentDIntroCutscene>
        {
            private double _timer;
            private float _originalMusicVolume;

            public override void OnStateEntered()
            {
                // HACK: Setting the background music volume to 0 instead of
                // calling Stop() to avoid a conflict with BackgroundMusicPlayer's
                // delayed-start logic.
                //
                // If we were to call Stop() instead, then BackgroundMusicPlayer
                // would just turn itself back on again after the start delay.
                //
                // TODO: Find a better way to orchestrate this.
                // (Yes, that was a pun.)
                _originalMusicVolume = Self.BackgroundMusicPlayer.VolumeLinear;
                Self.BackgroundMusicPlayer.VolumeLinear = 0;

                Self.CutsceneCamera.MakeCurrent();
                Self.AnimationPlayer.Play("Play");
                _timer = Self.AnimationPlayer.CurrentAnimationLength;

                var player = GetTree().FindNode<Player>();
                player.ChangeState<PlayerManhandledState>();
            }

            public override void OnStateExited()
            {
                Self.AnimationPlayer.Play("RESET");
                Self.BackgroundMusicPlayer.VolumeLinear = _originalMusicVolume;

                var player = GetTree().FindNode<Player>();
                player.ChangeState<PlayerWalkState>();
                player.Camera.MakeCurrent();

                Self.GetLevel().GetProgress().StoryFlags.Add(StoryFlag);
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

            private float _originalMusicVolume;

            public override void OnStateEntered()
            {
                _originalMusicVolume = Self.BackgroundMusicPlayer.VolumeLinear;
                Self.BackgroundMusicPlayer.VolumeLinear = 0;

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

                Self.BackgroundMusicPlayer.VolumeLinear = _originalMusicVolume;
                Self.BackgroundMusicPlayer.Stop();
                Self.BackgroundMusicPlayer.Play();
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