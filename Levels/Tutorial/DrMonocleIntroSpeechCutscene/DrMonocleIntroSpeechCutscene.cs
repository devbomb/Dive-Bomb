using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class DrMonocleIntroSpeechCutscene : Node
    {
        [ExportCategory("Internal")]
        [Export] public AnimationPlayer AnimationPlayer;
        [Export] public BackgroundMusicPlayer BackgroundMusicPlayer;
        [Export] public AudioStream EscapeMusic;

        [Export] public NamedTriggerZoneListener StartSpeechTrigger;
        [Export] public NamedTriggerZoneListener SkipSpeechTrigger;

        private static class StoryFlags
        {
            public const string SpeechSeen = "DrMonocleIntroSpeechSeen";
            public const string SpeechCheckpointed = "DrMonocleIntroSpeechCheckpointed";
        }

        private StateMachine _stateMachine = new();

        private const string ExitDoorId = "DrMonocleSpeech_ExitDoor";
        private IPowerable _exitDoor;

        private const string EntranceDoorId = "DrMonocleSpeech_EntranceDoor";
        private IPowerable _entranceDoor;

        public DrMonocleIntroSpeechCutscene()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Callable.From(() =>
            {
                _entranceDoor = this.FindPowerable(EntranceDoorId);
                _exitDoor = this.FindPowerable(ExitDoorId);

                Reset();
            }).CallDeferred();

        }

        private void Reset()
        {
            if (IsFlagSet(StoryFlags.SpeechCheckpointed))
                _stateMachine.ChangeState<Finished>();
            else
                _stateMachine.ChangeState<Idle>();
        }

        private bool IsFlagSet(string storyFlag) => this.GetLevel()
            .GetProgress()
            .StoryFlags
            .Contains(storyFlag);

        private void SetFlag(string storyFlag)
        {
            this.GetLevel()
                .GetProgress()
                .StoryFlags
                .Add(storyFlag);

            SaveFileManager.Instance.RequestAutosave();
        }

        private class Idle : State<DrMonocleIntroSpeechCutscene>
        {
            public override void OnStateEntered()
            {
                Self.AnimationPlayer.Play("RESET");
                Self.AnimationPlayer.Advance(0);
                Self.AnimationPlayer.Play("Hidden");

                Self._entranceDoor.InstantOpen();
                Self._exitDoor.InstantClose();
            }

            public override void SubscribeToSignals()
            {
                Self.StartSpeechTrigger.NamedTriggerEntered += StartSpeech;
            }

            public override void UnsubscribeFromSignals()
            {
                Self.StartSpeechTrigger.NamedTriggerEntered -= StartSpeech;
            }

            private void StartSpeech()
            {
                // Skip straight to the end of the speech if the player
                // has already seen it.
                if (Self.IsFlagSet(StoryFlags.SpeechSeen) || Self.IsTimeTrialMode())
                {
                    ChangeState<Finished>();
                    return;
                }

                ChangeState<Playing>();
            }
        }

        private class Playing : State<DrMonocleIntroSpeechCutscene>
        {
            private double _timer;
            public override void OnStateEntered()
            {
                GD.Print("Dr. Monocle speech started");

                Self.AnimationPlayer.Play("Playing");
                Self.BackgroundMusicPlayer.Stop();
                _timer = Self.AnimationPlayer.CurrentAnimationLength;

                Self._entranceDoor.StartClosing();
            }

            public override void SubscribeToSignals()
            {
                Self.SkipSpeechTrigger.NamedTriggerEntered += SkipSpeech;
            }

            public override void UnsubscribeFromSignals()
            {
                Self.SkipSpeechTrigger.NamedTriggerEntered -= SkipSpeech;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                    ChangeState<Finished>();
            }

            private void SkipSpeech()
            {
                ChangeState<Skipping>();
            }
        }

        private class Skipping : State<DrMonocleIntroSpeechCutscene>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                GD.Print("Dr. Monocle speech skipping");
                Self.SetFlag(StoryFlags.SpeechSeen);

                Self.AnimationPlayer.Play("Skipping");

                _timer = Self.AnimationPlayer.CurrentAnimationLength;

                Self._entranceDoor.StartOpening();
                Self._exitDoor.StartOpening();
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
                Self.SetFlag(StoryFlags.SpeechCheckpointed);
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                    ChangeState<Finished>();
            }
        }

        private class Finished : State<DrMonocleIntroSpeechCutscene>
        {
            public override void OnStateEntered()
            {
                GD.Print("Dr. Monocle speech finished");
                Self.SetFlag(StoryFlags.SpeechSeen);

                Self.AnimationPlayer.Play("Hidden");

                Self._entranceDoor.StartOpening();
                Self._exitDoor.StartOpening();

                Self.BackgroundMusicPlayer.Stream = Self.EscapeMusic;
                Self.BackgroundMusicPlayer.Play();
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
                Self.SetFlag(StoryFlags.SpeechCheckpointed);
            }
        }
    }
}