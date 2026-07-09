using System;
using Godot;

namespace FastDragon.Levels.Tutorial
{
    public partial class DrMonocleIntroSpeechCutscene : Node
    {
        [Export] public string targetname;

        [Export] public OpenableWall EntranceDoor;
        [Export] public OpenableWall ExitDoor;

        [ExportCategory("Internal")]
        [Export] public AnimationPlayer AnimationPlayer;
        [Export] public Node3D HeadHolder;
        [Export(PropertyHint.Range, "0,1")]
        public float LookAtPlayerPitchInfluence = 1;

        public bool IsPlaying => _stateMachine.CurrentState is not (Idle or Finished);

        private event Action _selfDestructButtonPressed;

        private readonly StateMachine _stateMachine = new();


        private Player _player;

        public DrMonocleIntroSpeechCutscene()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            _player = GetTree().FindNode<Player>();
            // You'll notice we're not subscribing to LevelReset here.
            // That's intentional.  This guy's reset behavior is controlled by
            // TutorialStoryManager.
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

        public void GoToIdle()
        {
            _stateMachine.ChangeState<Idle>();
        }

        public void StartPlaying()
        {
            _stateMachine.ChangeState<Playing>();
        }

        public void GoToFinished()
        {
            _stateMachine.ChangeState<Finished>();
        }

        public void OnSelfDestructButtonPressed()
        {
            _selfDestructButtonPressed?.Invoke();
        }

        private class Idle : State<DrMonocleIntroSpeechCutscene>
        {
            public override void OnStateEntered()
            {
                Self.AnimationPlayer.Play("RESET");
                Self.AnimationPlayer.Advance(0);
                Self.AnimationPlayer.Play("Hidden");

                Self.EntranceDoor.InstantOpen();
                Self.ExitDoor.InstantClose();
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

                Self.EntranceDoor.StartClosing();
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
                Self.AnimationPlayer.Play("Hidden");

                Self.EntranceDoor.StartOpening();
                Self.ExitDoor.StartOpening();
            }
        }
    }
}