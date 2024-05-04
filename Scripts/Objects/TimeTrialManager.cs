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
        public double TargetTime {get; private set;}

        private Label _timerLabel => GetNode<Label>("%TimerLabel");

        private PageNavigator _pageNav => GetNode<PageNavigator>("%PageNavigator");
        private Page _briefingPage => GetNode<Page>("%TimeTrialBriefingPage");
        private Page _resultsPage => GetNode<Page>("%TimeTrialResultsPage");

        private bool _isResettingSaveFile = false;

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

            if (_isResettingSaveFile)
            {
                _isResettingSaveFile = false;
                return;
            }

            Timer = 0;
            TargetTime = GetSavedBestTime();

            IsTimerRunning = false;
            _pageNav.ChangePage(_briefingPage);

            // Reset the save file, to respawn any collectables that may have
            // been collected on the previous attempt.
            string currentMap = SaveFile.Current.CurrentMap;
            SaveFile.Current = new SaveFile();
            SaveFile.Current.CurrentMap = currentMap;

            // HACK: We don't technically know which order the LevelReset
            // handlers will run in.  Some gems may have already reset
            // themselves based on the previous save file before we had time to
            // swap it.  So, let's fire the reset event one more time to ensure
            // EVERYONE sees the clean save file.
            //
            // HACK: Need to call deferred while doing this to avoid an
            // InvalidOperationException, due to FakeSignal not supporting
            // reentrancy.
            _isResettingSaveFile = true;
            SignalBus.Instance.CallDeferred(nameof(SignalBus.Instance.EmitLevelReset));
        }

        public void Start()
        {
            _pageNav.ChangePage(null);
            IsTimerRunning = true;
            GetTree().Paused = false;
        }

        public void Finish()
        {
            if (!IsTimeTrialMode)
                return;

            IsTimerRunning = false;
            _pageNav.ChangePage(_resultsPage);

            if (Timer < GetSavedBestTime())
                SetSavedBestTime(Timer);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsTimerRunning && IsTimeTrialMode && !GetTree().Paused)
                Timer += delta;

            _timerLabel.Visible = IsTimeTrialMode;
            _timerLabel.Text = TimeUtils.FormatStopwatch(Timer);
        }

        private double GetSavedBestTime()
        {
            // TODO: Use a different time based on the current mode
            double devTime = GetTree().FindNode<Player>().AnyPercentDevTime;
            return CurrentMapEntry().AnyPercentRecord ?? devTime;
        }

        private void SetSavedBestTime(double time)
        {
            // TODO: Take the time trial mode into account
            CurrentMapEntry().AnyPercentRecord = time;
            TimeTrialSaveData.Instance.SaveToJson();
        }

        private TimeTrialSaveData.Entry CurrentMapEntry()
        {
            return TimeTrialSaveData.Instance.GetEntry(SaveFile.Current.CurrentMap);
        }
    }
}