using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class TutorialStoryManager : Node
    {
        [Export] public string targetname;

        [Export] public BackgroundSong EscapeMusic;

        [Export] public BackgroundMusicPlayer MusicPlayer;
        [Export] public AgentDIntroCutscene AgentDIntro;
        [Export] public DrMonocleIntroSpeechCutscene DrMonocleIntro;

        public static class StoryFlags
        {
            public static readonly StoryFlag AgentDIntroFinished = StoryFlag.Permanent("AgentDIntroFinished");
            public static readonly StoryFlag EscapeSequenceStarted = StoryFlag.Checkpointable("EscapeSequenceStarted");
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

                if (this.IsStoryFlagSet(StoryFlags.EscapeSequenceStarted))
                {
                    _stateMachine.ChangeState<EscapeSequence>();
                    return;
                }

                bool seenAgentDIntro = this.IsStoryFlagSet(StoryFlags.AgentDIntroFinished);
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
            public override void OnStateEntered()
            {
                Self.AgentDIntro.Play();
                Self.MusicPlayer.Stop();
            }

            public override void OnStateExited()
            {
                Self.SetStoryFlag(StoryFlags.AgentDIntroFinished);
                Self.MusicPlayer.RestartSong();
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

            public override void OnStateExited()
            {
                Self.MusicPlayer.RestartSong();
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
                Self.MusicPlayer.OverrideSong(Self.EscapeMusic);
                Self.DrMonocleIntro.GoToFinished();

                // This needs to be set AFTER GoToFinished(), because that
                // method checks the flag to determine if we're reloading a
                // checkpoint or not.
                Self.SetStoryFlag(StoryFlags.EscapeSequenceStarted);
            }

            public override void OnStateExited()
            {
                Self.MusicPlayer.RemoveSongOverride();
            }
        }
    }
}
