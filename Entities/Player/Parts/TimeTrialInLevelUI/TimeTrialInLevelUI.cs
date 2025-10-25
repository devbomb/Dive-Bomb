using System;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialInLevelUI : Control
    {
        public bool IsTimeTrialMode => this.GetLevel()?.TimeTrial?.IsTimeTrialMode ?? false;
        public bool IsTimerRunning => this.GetLevel()?.TimeTrial?.IsTimerRunning ?? false;


        private Label _timerLabel => GetNode<Label>("%TimerLabel");
        private AnimationPlayer _timeAnnouncementAnimator => GetNode<AnimationPlayer>("%TimeAnnouncementAnimator");

        private PageNavigator _pageNav => GetNode<PageNavigator>("%PageNavigator");
        private Page _briefingPage => GetNode<Page>("%TimeTrialBriefingPage");
        private Page _resultsPage => GetNode<Page>("%TimeTrialResultsPage");

        public override void _Ready()
        {
            ProcessMode = ProcessModeEnum.Always;

            var level = this.GetLevel();
            if (level != null)
            {
                level.TimeTrial.ReadyToShowBriefing += ShowBriefing;
                level.TimeTrial.TimeTrialFinished += OnExitReached;
                level.TimeTrial.ReadyToShowResults += ShowResultsScreen;
            }

            _pageNav.ChangePage(null);
        }

        private void ShowBriefing()
        {
            _pageNav.ChangePage(_briefingPage);
        }

        private void OnExitReached()
        {
            _timeAnnouncementAnimator.Play("TIME");
        }

        public void ShowResultsScreen()
        {
            _pageNav.ChangePage(_resultsPage);
        }

        public void Start()
        {
            _pageNav.ChangePage(null);
            this.GetLevel().TimeTrial.Start();
        }

        public void ExitTimeTrialMode()
        {
            _pageNav.ChangePage(null);
            GetTree().Paused = false;
            this.GetLevel().TimeTrial.ExitTimeTrialMode();
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsTimeTrialMode)
            {
                var ticks = this.GetLevel().TimeTrial.Timer;
                _timerLabel.Text = ticks.FormatStopwatch();
                _timerLabel.Visible = true;
            }
            else
            {
                _timerLabel.Visible = false;
            }
        }
    }
}