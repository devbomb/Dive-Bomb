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
        [Export] public Node3D HeadHolder;
        [Export(PropertyHint.Range, "0,1")]
        public float LookAtPlayerPitchInfluence = 1;

        private event Action _startSpeechRequested;
        private event Action _selfDestructButtonPressed;

        private static class StoryFlags
        {
            public const string SpeechCheckpointed = "DrMonocleIntroSpeechCheckpointed";
        }

        private readonly StateMachine _stateMachine = new();

        private const string ExitDoorId = "DrMonocleSpeech_ExitDoor";
        private IPowerable _exitDoor;

        private const string EntranceDoorId = "DrMonocleSpeech_EntranceDoor";
        private IPowerable _entranceDoor;

        private Player _player;

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
                _player = GetTree().FindNode<Player>();

                Reset();
            }).CallDeferred();

        }

        public override void _Process(double delta)
        {
            LookAtPlayer();
        }

        private void LookAtPlayer()
        {
            // HACK: Avoid errors due to the quaternion not being normalized
            // when the animation has set the scale to 0
            if (HeadHolder.Scale.IsZeroApprox())
                return;

            // Get a transform where he's looking directly at the player
            HeadHolder.LookAt(_player.GlobalPosition);
            var lookingStraightAtPlayer = HeadHolder.GlobalTransform;

            // Get a transform where he's looking at the player, but his head
            // is parallel to the floor
            var rot = HeadHolder.GlobalRotation;
            rot.X = 0;
            rot.Z = 0;
            HeadHolder.GlobalRotation = rot;
            var parallelToFloor = HeadHolder.GlobalTransform;

            // Interpolate between the two according to the pitch influence
            HeadHolder.GlobalTransform = parallelToFloor.InterpolateWith(
                lookingStraightAtPlayer,
                LookAtPlayerPitchInfluence
            );
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
            if (this.GetLevel().TempStoryFlags.Contains(StoryFlags.SpeechCheckpointed))
                _stateMachine.ChangeState<Finished>();
            else
                _stateMachine.ChangeState<Idle>();
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
                Self.AnimationPlayer.Play("Skipping", 0.25);
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
                Self.GetLevel().TempStoryFlags.Add(StoryFlags.SpeechCheckpointed);
            }
        }
    }
}