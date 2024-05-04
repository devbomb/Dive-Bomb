using Godot;

namespace FastDragon
{
    public partial class TimeTrialResultsMenu : Page
    {
        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%Animator");
        private Control _buttons => GetNode<Control>("%Buttons");

        private Label _yourTimeLabel => GetNode<Label>("%YourTimeLabel");
        private Label _bestTimeLabel => GetNode<Label>("%BestTimeLabel");

        public override void OnPageEntered()
        {
            var timeTrialManager = GetTree().FindNode<TimeTrialManager>();
            _yourTimeLabel.Text = TimeUtils.FormatStopwatch(timeTrialManager.Timer);
            _bestTimeLabel.Text = TimeUtils.FormatStopwatch(timeTrialManager.TargetTime);

            _buttons.Visible = false;

            _animator.Play("RESET");
            _animator.Advance(0);

            _animator.Play("Open");

            if (timeTrialManager.Timer < timeTrialManager.TargetTime)
                _animator.Queue("NewHighScore");

            _animator.AnimationFinished += (StringName animName) =>
            {
                _buttons.Visible = true;
                FocusedControl.GrabFocus();
            };
        }

        public void OnRetryPressed() => MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        public void OnQuitToTitlePressed() => MapTransitionManager.Instance.GoToTitleScreen();
    }
}