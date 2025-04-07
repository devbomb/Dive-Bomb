using Godot;

namespace FastDragon
{
    public partial class TimeTrialResultsMenu : Page
    {
        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%Animator");
        private Control _buttons => GetNode<Control>("%Buttons");

        private Label _yourTimeLabel => GetNode<Label>("%YourTimeLabel");
        private Label _bestTimeLabel => GetNode<Label>("%BestTimeLabel");

        public override void _Ready()
        {
            _animator.AnimationFinished += (StringName animName) =>
            {
                _buttons.Visible = true;
                FocusedControl.GrabFocus();
            };
        }

        public override void OnPageEntered()
        {
            var timeTrialManager = GetTree().FindNode<TimeTrialManager>();
            _yourTimeLabel.Text = TimeUtils.FormatPhysicsTicksStopwatch(timeTrialManager.TimerPhysicsTicks);
            _bestTimeLabel.Text = TimeUtils.FormatPhysicsTicksStopwatch(timeTrialManager.TargetTimePhysicsTicks);

            _buttons.Visible = false;

            _animator.Play("RESET");
            _animator.Advance(0);

            _animator.Play("Open");

            if (timeTrialManager.TimerPhysicsTicks < timeTrialManager.TargetTimePhysicsTicks)
                _animator.Queue("NewHighScore");
        }

        public void OnRetryPressed() => MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        public void OnQuitToTitlePressed() => MapTransitionManager.Instance.GoToTitleScreen();
    }
}