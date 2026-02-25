using System;
using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class DrMonocleIntroSpeechCutscene : Node
    {
        [Export] public string targetname;

        [Export] public AudioStreamPlayer MusicPlayer;
        private AudioStreamPlaybackInteractive _musicPlayback => (AudioStreamPlaybackInteractive)MusicPlayer.GetStreamPlayback();

        [ExportCategory("Internal")]
        [Export] public AnimationPlayer AnimationPlayer;

        private event Action _startSpeechRequested;
        private event Action _selfDestructButtonPressed;

        private static class StoryFlags
        {
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
                _entranceDoor = this.FindNodeByTargetName<IPowerable>(EntranceDoorId);
                _exitDoor = this.FindNodeByTargetName<IPowerable>(ExitDoorId);

                Reset();
            }).CallDeferred();

        }

        public void StartSpeech(Node3D body)
        {
            _startSpeechRequested?.Invoke();
        }

        public void OnSelfDestructButtonPressed()
        {
            _selfDestructButtonPressed?.Invoke();
        }

        private void Reset()
        {
            if (IsFlagSet(StoryFlags.SpeechCheckpointed))
                _stateMachine.ChangeState<Finished>();
            else
                _stateMachine.ChangeState<Idle>();
        }

        private bool IsFlagSet(string storyFlag) => SaveFileManager
            .Current
            .CurrentLevelVisit
            .StoryFlags
            .Contains(storyFlag);

        private void SetFlag(string storyFlag)
        {
            SaveFileManager
                .Current
                .CurrentLevelVisit
                .StoryFlags
                .Add(storyFlag);

            SaveFileManager.Instance.RequestAutosave();
        }

        private class Idle : State<DrMonocleIntroSpeechCutscene>
        {
            public override void OnStateEntered()
            {
                // TODO: Find a more "correct" way of avoiding this issue.
                // The issue is that this node and the Agent D. intro cutscene
                // are both fighting over control of the background music
                // player.  Agent D. wants to stop the music, while Dr. Monocle
                // wants to play something.  I'm sure there's a metaphor in
                // there somewhere.
                if (Self.MusicPlayer.Playing)
                {
                    Self._musicPlayback?.SwitchToClipByName("Normal");
                }

                Self.AnimationPlayer.Play("RESET");
                Self.AnimationPlayer.Advance(0);
                Self.AnimationPlayer.Play("Hidden");

                Self._entranceDoor.InstantOpen();
                Self._exitDoor.InstantClose();
            }

            public override void SubscribeToSignals()
            {
                Self._startSpeechRequested += StartSpeech;
            }

            public override void UnsubscribeFromSignals()
            {
                Self._startSpeechRequested -= StartSpeech;
            }

            private void StartSpeech()
            {
                ChangeState<Playing>();
            }
        }

        private class Playing : State<DrMonocleIntroSpeechCutscene>
        {
            private double _timer;
            public override void OnStateEntered()
            {
                GD.Print("Dr. Monocle speech started");
                Self.MusicPlayer.Stop();

                Self.AnimationPlayer.Play("Playing");
                _timer = Self.AnimationPlayer.CurrentAnimationLength;

                Self._entranceDoor.StartClosing();
            }

            public override void SubscribeToSignals()
            {
                Self._selfDestructButtonPressed += SkipSpeech;
            }

            public override void UnsubscribeFromSignals()
            {
                Self._selfDestructButtonPressed -= SkipSpeech;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                    ChangeState<WaitingForSelfDestructButton>();
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
                Self.AnimationPlayer.Play("Skipping");
                _timer = Self.AnimationPlayer.CurrentAnimationLength;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                    ChangeState<Finished>();
            }
        }

        private class WaitingForSelfDestructButton : State<DrMonocleIntroSpeechCutscene>
        {
            public override void SubscribeToSignals()
            {
                Self._selfDestructButtonPressed += FinishCutscene;
            }

            public override void UnsubscribeFromSignals()
            {
                Self._selfDestructButtonPressed -= FinishCutscene;
            }

            private void FinishCutscene()
            {
                ChangeState<CheckingBackIn>();
            }
        }

        private class CheckingBackIn : State<DrMonocleIntroSpeechCutscene>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                Self.AnimationPlayer.Play("CheckingBackIn");
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
                Self.MusicPlayer.Play();
                Self._musicPlayback.SwitchToClipByName("Escape");

                Self.AnimationPlayer.Play("Hidden");

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
        }
    }
}