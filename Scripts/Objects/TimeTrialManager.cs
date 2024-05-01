using System;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialManager : Node
    {
        public bool IsTimeTrialMode => Mode != TimeTrialMode.None;
        public bool IsTimerRunning {get; private set;} = false;

        public TimeTrialMode Mode {get; private set;} = TimeTrialMode.None;
        public enum TimeTrialMode
        {
            None,
            AnyPercent
        }

        public double Timer {get; private set;}

        private PageNavigator _pageNav => GetNode<PageNavigator>("%PageNavigator");
        private Page _briefingPage => GetNode<Page>("%TimeTrialBriefingPage");

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += OnLevelReset;
            _pageNav.ChangePage(null);
        }

        public void Initialize(TimeTrialMode mode)
        {
            Mode = mode;
            ProcessMode = ProcessModeEnum.Always;
        }

        public void OnLevelReset()
        {
            if (!IsTimeTrialMode)
                return;

            Timer = 0;
            IsTimerRunning = false;
            _pageNav.ChangePage(_briefingPage);
        }

        public void Start()
        {
            _pageNav.ChangePage(null);
            IsTimerRunning = true;
            GetTree().Paused = false;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsTimerRunning && IsTimeTrialMode && !GetTree().Paused)
                Timer += delta;
        }
    }
}