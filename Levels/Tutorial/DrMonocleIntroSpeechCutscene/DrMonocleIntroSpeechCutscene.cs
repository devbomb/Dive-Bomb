using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class DrMonocleIntroSpeechCutscene : Node
    {
        [ExportCategory("Internal")]
        [Export] public AnimationPlayer AnimationPlayer;

        private const string StoryFlagId = "DrMonocleIntroSpeechCutsceneFinished";

        private StateMachine _stateMachine = new();

        public DrMonocleIntroSpeechCutscene()
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
            }
        }

        private class Playing : State<DrMonocleIntroSpeechCutscene>
        {
            private double _timer;
            public override void OnStateEntered()
            {
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
                Self.SetStoryFlag();
                Self.AnimationPlayer.Play("Hidden");
                GD.Print("Cutscene finished.  TODO: Open the door");
            }
        }
    }
}