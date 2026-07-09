using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class TutorialStoryManager : Node
    {
        [Export] public string targetname;

        [Export] public BackgroundMusicPlayer MusicPlayer;
        [Export] public AgentDIntroCutscene AgentDIntro;
        [Export] public DrMonocleIntroSpeechCutscene DrMonocleIntro;

        private AudioStreamPlaybackInteractive _musicPlayback => (AudioStreamPlaybackInteractive)MusicPlayer.GetStreamPlayback();

        private static class StoryFlags
        {
            public const string AgentDIntroFinished = "AgentDIntroFinished";
            public const string DrMonocleSpeechCheckpointed = "DrMonocleIntroSpeechCheckpointed";
        }

        private readonly StateMachine _stateMachine = new();

        private event System.Action _startDrMonocleSpeechRequested;

        public TutorialStoryManager()
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
            // HACK: Defer this to ensure it runs AFTER all of the individual
            // cutscenes reset.
            Callable.From(() =>
            {
                if (this.IsTimeTrialMode())
                {
                    _stateMachine.ChangeState<SneakyTime>();
                    return;
                }

                bool startedEscapeSequence = this.GetLevel()
                    .TempStoryFlags
                    .Contains(StoryFlags.DrMonocleSpeechCheckpointed);

                if (startedEscapeSequence)
                {
                    _stateMachine.ChangeState<EscapeSequence>();
                    return;
                }

                bool seenAgentDIntro = this.GetLevel()
                    .PermanentStoryFlags
                    .Contains(StoryFlags.AgentDIntroFinished);

                if (!seenAgentDIntro && !this.PlaySceneFromHereWasUsed())
                {
                    _stateMachine.ChangeState<PlayingAgentDIntro>();
                    return;
                }

                _stateMachine.ChangeState<SneakyTime>();
            }).CallDeferred();
        }

        public void RequestStartDrMonocleSpeech(Node3D body)
        {
            _startDrMonocleSpeechRequested?.Invoke();
        }


        private partial class PlayingAgentDIntro : State<TutorialStoryManager>
        {
            private float _originalMusicVolume;

            public override void OnStateEntered()
            {
                Self.AgentDIntro.Play();

                // HACK: Setting the background music volume to 0 instead of
                // calling Stop() to avoid a conflict with BackgroundMusicPlayer's
                // delayed-start logic.
                //
                // If we were to call Stop() instead, then BackgroundMusicPlayer
                // would just turn itself back on again after the start delay.
                //
                // TODO: Find a better way to orchestrate this.
                // (Yes, that was a pun.)
                _originalMusicVolume = Self.MusicPlayer.VolumeLinear;
                Self.MusicPlayer.VolumeLinear = 0;
            }

            public override void OnStateExited()
            {
                Self.GetLevel().PermanentStoryFlags.Add(StoryFlags.AgentDIntroFinished);

                Self.MusicPlayer.VolumeLinear = _originalMusicVolume;
                Self.MusicPlayer.Stop();
                Self.MusicPlayer.Play();
            }

            public override void _PhysicsProcess(double delta)
            {
                if (!Self.AgentDIntro.IsPlaying)
                    ChangeState<SneakyTime>();
            }
        }

        private partial class SneakyTime : State<TutorialStoryManager>
        {
            public override void OnStateEntered()
            {
                if (Self.MusicPlayer.Playing)
                {
                    Self._musicPlayback?.SwitchToClipByName("Normal");
                }

                Self.DrMonocleIntro.GoToIdle();
            }

            public override void SubscribeToSignals()
            {
                Self._startDrMonocleSpeechRequested += StartDrMonocleSpeech;
            }

            public override void UnsubscribeFromSignals()
            {
                Self._startDrMonocleSpeechRequested -= StartDrMonocleSpeech;
            }

            private void StartDrMonocleSpeech()
            {
                ChangeState<PlayingDrMonocleSpeech>();
            }
        }

        private partial class PlayingDrMonocleSpeech : State<TutorialStoryManager>
        {
            public override void OnStateEntered()
            {
                Self.MusicPlayer.Stop();
                Self.DrMonocleIntro.StartPlaying();
            }

            public override void _PhysicsProcess(double delta)
            {
                if (!Self.DrMonocleIntro.IsPlaying)
                    ChangeState<EscapeSequence>();
            }
        }

        private partial class EscapeSequence : State<TutorialStoryManager>
        {
            public override void OnStateEntered()
            {
                GD.Print("Starting escape sequence");
                Self.MusicPlayer.Play();
                Self._musicPlayback.SwitchToClipByName("Escape");
            }

            public override void SubscribeToSignals()
            {
                SignalBus.Instance.CheckpointActivated += CheckpointActivated;
            }

            public override void UnsubscribeFromSignals()
            {
                SignalBus.Instance.CheckpointActivated -= CheckpointActivated;
            }

            private void CheckpointActivated()
            {
                GD.Print("Dr. Monocle speech checkpointed");
                Self.GetLevel().TempStoryFlags.Add(StoryFlags.DrMonocleSpeechCheckpointed);
            }
        }
    }
}
