using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class DrMonocleIntroSpeechCutscene : Node
    {
        [ExportCategory("Internal")]
        [Export] public AnimationPlayer AnimationPlayer;

        private const string StoryFlagId = "DrMonocleIntroSpeechCutsceneFinished";

        private StateMachine _stateMachine = new();

        private const string ExitDoorId = "DrMonocleSpeech_ExitDoor";
        private OpenableWall _exitDoor;

        private const string EntranceDoorId = "DrMonocleSpeech_EntranceDoor";
        private OpenableWall _entranceDoor;

        public DrMonocleIntroSpeechCutscene()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Callable.From(() =>
            {
                _entranceDoor = this.GetOpenableWall(EntranceDoorId);
                _exitDoor = this.GetOpenableWall(ExitDoorId);

                Reset();
            }).CallDeferred();

        }

        private void Reset()
        {
            if (IsStoryFlagSet())
                _stateMachine.ChangeState<Finished>();
            else
                _stateMachine.ChangeState<Idle>();
        }

        public void StartCutscene()
        {
            if (_stateMachine.CurrentState is Idle)
            {
                if (IsStoryFlagSet() || this.IsTimeTrialMode())
                    _stateMachine.ChangeState<Finished>();
                else
                    _stateMachine.ChangeState<Playing>();
            }
        }

        public void SkipCutscene()
        {
            if (_stateMachine.CurrentState is Playing)
            {
                _stateMachine.ChangeState<Skipping>();
            }
        }

        private bool IsStoryFlagSet() => this.GetLevel()
            .GetProgress()
            .StoryFlags
            .Contains(StoryFlagId);

        private void SetStoryFlag()
        {
            this.GetLevel()
                .GetProgress()
                .StoryFlags
                .Add(StoryFlagId);

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
        }

        private class Playing : State<DrMonocleIntroSpeechCutscene>
        {
            private double _timer;
            public override void OnStateEntered()
            {
                GD.Print("Dr. Monocle speech started");

                Self.AnimationPlayer.Play("Playing");
                _timer = Self.AnimationPlayer.CurrentAnimationLength;

                Self._entranceDoor.StartClosing();
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                    ChangeState<Finished>();
            }
        }

        private class Skipping : State<DrMonocleIntroSpeechCutscene>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                GD.Print("Dr. Monocle speech skipping");
                Self.SetStoryFlag();
                Self.AnimationPlayer.Play("Skipping");

                _timer = Self.AnimationPlayer.CurrentAnimationLength;

                Self._entranceDoor.StartOpening();
                Self._exitDoor.StartOpening();
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
                Self.SetStoryFlag();
                Self.AnimationPlayer.Play("Hidden");

                Self._entranceDoor.StartOpening();
                Self._exitDoor.StartOpening();
            }
        }
    }
}