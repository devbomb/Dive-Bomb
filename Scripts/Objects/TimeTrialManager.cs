using System;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialManager : Node
    {
        public bool IsTimeTrialMode => Mode != null;
        public bool IsTimerRunning {get; private set;} = false;

        public TimeTrialCategory? Mode {get; private set;} = null;

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
            SignalBus.Instance.ExitReached += OnExitReached;

            _pageNav.ChangePage(null);
        }

        public void Initialize(TimeTrialCategory mode)
        {
            Mode = mode;
            ProcessMode = ProcessModeEnum.Always;
        }

        public bool RequirementsMet()
        {
            switch (Mode)
            {
                case TimeTrialCategory.FairyPercent:
                {
                    var saveFile = SaveFile.Current;
                    var mapEntry = AtlasCache.Instance.GetEntry(saveFile.CurrentMap);
                    int fairiesFound = saveFile.CurrentMapProgress.CollectedFairies.Count;

                    return fairiesFound >= mapEntry.TotalFairiesInLevel;
                }

                default: return true;
            }
        }

        private void OnLevelReset()
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

        private void OnExitReached()
        {
            Finish();

            // Unlock time trial modes
            // TODO: Only do this if currently NOT in time trial mode
            string currentMap = SaveFile.Current.CurrentMap;
            var mapProgress = SaveFile.Current.CurrentMapProgress;
            var atlasEntry = AtlasCache.Instance.GetEntry(currentMap);

            bool levelHasGems = atlasEntry.TotalGemsInLevel > 0;
            bool levelHasFairies = atlasEntry.TotalFairiesInLevel > 0;

            TimeTrialSaveData.Instance.UnlockCategory(currentMap, TimeTrialCategory.AnyPercent);

            if (levelHasFairies && mapProgress.FairiesCollected >= atlasEntry.TotalFairiesInLevel)
                TimeTrialSaveData.Instance.UnlockCategory(currentMap, TimeTrialCategory.FairyPercent);
        }

        public void Start()
        {
            _pageNav.ChangePage(null);
            IsTimerRunning = true;
            GetTree().Paused = false;
        }

        private void Finish()
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
            {
                // Divide by the time scale to cancel out slow-motion effects
                // (such as hitstop), allowing us to keep track of real-life
                // time(or something close to it.)
                //
                // If you think this is unfair, consider getting good.
                Timer += delta / Engine.TimeScale;
            }

            _timerLabel.Visible = IsTimeTrialMode;
            _timerLabel.Text = TimeUtils.FormatStopwatch(Timer);
        }

        private double GetSavedBestTime()
        {
            if (CurrentCategoryEntry().Record != null)
                return CurrentCategoryEntry().Record.Value;

            var player = GetTree().FindNode<Player>();

            switch (Mode)
            {
                case TimeTrialCategory.AnyPercent: return player.AnyPercentDevTime;
                case TimeTrialCategory.FairyPercent: return player.FairyPercentDevTime;
                default: throw new Exception($"Unknown time trial mode {Mode}");
            }
        }

        private void SetSavedBestTime(double time)
        {
            CurrentCategoryEntry().Record = time;
            TimeTrialSaveData.Instance.SaveToJson();
        }

        private TimeTrialSaveData.CategoryEntry CurrentCategoryEntry()
        {
            return TimeTrialSaveData
                .Instance
                .GetEntry(SaveFile.Current.CurrentMap, Mode.Value);
        }
    }
}