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

        public DrMonocleIntroSpeechCutscene()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Callable.From(() =>
            {
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

                Self._exitDoor.InstantClose();
            }
        }

        private class Playing : State<DrMonocleIntroSpeechCutscene>
        {
            private double _timer;
            public override void OnStateEntered()
            {
                GD.Print("Dr. Monocle speech finished");

                Self.AnimationPlayer.Play("Playing");
                _timer = Self.AnimationPlayer.CurrentAnimationLength;
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

                Self._exitDoor.StartOpening();
            }
        }
    }
}