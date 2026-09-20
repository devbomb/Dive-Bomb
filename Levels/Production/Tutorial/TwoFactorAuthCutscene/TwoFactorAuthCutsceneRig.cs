using Godot;
using System;

namespace FastDragon.Levels.Tutorial
{
    public partial class TwoFactorAuthCutsceneRig : Node
    {
        private static readonly StoryFlag JokeFinishedFlag = StoryFlag.Checkpointable("EscapeSequence_2FA_Joke_Finished");

        [Export] public string targetname;
        [Export] public OpenableWall Door;

        [ExportGroup("Internal")]
        [Export] public AnimationPlayer Animator;

        private event Action _triggerEntered;
        private event Action _authenticateButtonPressed;

        private readonly StateMachine _stateMachine = new();

        public override void _Ready()
        {
            AddChild(_stateMachine);
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            bool jokeFinished = this.IsStoryFlagSet(JokeFinishedFlag);

            if (jokeFinished)
                _stateMachine.ChangeState<AllDone>();
            else
                _stateMachine.ChangeState<Idle>();
        }

        public void OnBodyEntered(Node3D body) => _triggerEntered?.Invoke();
        public void OnAuthenticateButtonPressed() => _authenticateButtonPressed?.Invoke();

        private class Idle : State<TwoFactorAuthCutsceneRig>
        {
            public override void OnStateEntered()
            {
                Self.Animator.Play("RESET");
                Self.Door.InstantClose();
            }

            public override void SubscribeToSignals()
            {
                Self._triggerEntered += OnTriggerEntered;
                Self._authenticateButtonPressed += OnAuthenticateButtonPressed;
            }

            public override void UnsubscribeFromSignals()
            {
                Self._triggerEntered -= OnTriggerEntered;
                Self._authenticateButtonPressed -= OnAuthenticateButtonPressed;
            }

            private void OnTriggerEntered() => ChangeState<CalmDownSpeech>();
            private void OnAuthenticateButtonPressed() => ChangeState<DoomedSpeech>();
        }

        private class CalmDownSpeech : State<TwoFactorAuthCutsceneRig>
        {
            public override void OnStateEntered()
            {
                Self.Animator.Play("CalmDown");
            }

            public override void SubscribeToSignals()
            {
                Self._authenticateButtonPressed += OnAuthenticateButtonPressed;
            }

            public override void UnsubscribeFromSignals()
            {
                Self._authenticateButtonPressed -= OnAuthenticateButtonPressed;
            }

            private void OnAuthenticateButtonPressed() => ChangeState<DoomedSpeech>();
        }

        private class DoomedSpeech : State<TwoFactorAuthCutsceneRig>
        {
            public override void OnStateEntered()
            {
                Self.SetStoryFlag(JokeFinishedFlag);
                Self.Animator.Play("Doomed");
                Self.Door.StartOpening();
            }
        }

        private class AllDone : State<TwoFactorAuthCutsceneRig>
        {
            public override void OnStateEntered()
            {
                Self.Door.InstantOpen();
            }
        }
    }
}
