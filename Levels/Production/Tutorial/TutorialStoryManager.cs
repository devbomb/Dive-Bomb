using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class TutorialStoryManager : Node
    {
        [Export] public BackgroundMusicPlayer MusicPlayer;
        [Export] public AgentDIntroCutscene AgentDIntro;
        [Export] public DrMonocleIntroSpeechCutscene DrMonocleIntro;

        private static class StoryFlags
        {
            public const string AgentDIntroFinished = "AgentDIntroFinished";
            public const string DrMonocleSpeechCheckpointed = "DrMonocleIntroSpeechCheckpointed";
        }

        private readonly StateMachine _stateMachine = new();

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
                if (this.IsTimeTrialMode() || this.PlaySceneFromHereWasUsed())
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

                if (!seenAgentDIntro)
                {
                    _stateMachine.ChangeState<PlayingAgentDIntro>();
                    return;
                }
            }).CallDeferred();
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

        }

        private partial class PlayingDrMonocleSpeech : State<TutorialStoryManager>
        {

        }

        private partial class EscapeSequence : State<TutorialStoryManager>
        {

        }
    }
}
