using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialManager : Node
    {
        public bool IsTimeTrialMode { get; private set; } = false;
        public bool IsTimerRunning { get; private set; } = false;

        public PhysicsTicks Timer {get; private set;}

        public LevelProgress DummyProgress { get; private set; } = new();

        [Signal] public delegate void ReadyToShowBriefingEventHandler();
        [Signal] public delegate void TimeTrialStartedEventHandler();
        [Signal] public delegate void TimeTrialFinishedEventHandler();
        [Signal] public delegate void ReadyToShowResultsEventHandler();

        private bool _isResettingLevelProgress = false;

        private Dictionary<TimeTrialCategory, PhysicsTicks> _targetTimes = new();

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += OnLevelReset;
            SignalBus.Instance.ExitReached += OnExitReached;
            ProcessMode = ProcessModeEnum.Always;
        }

        public void EnterTimeTrialMode()
        {
            IsTimeTrialMode = true;
            IsTimerRunning = false;
            _targetTimes = CopyBestTimes();

            SaveFileManager.Current.CurrentLevelVisit = new();
            SignalBus.Instance.EmitLevelReset();
        }

        public void ExitTimeTrialMode()
        {
            IsTimeTrialMode = false;
            IsTimerRunning = false;

            SaveFileManager.Current.CurrentLevelVisit = new();
            SignalBus.Instance.EmitLevelReset();
        }

        public bool RequirementsMet(TimeTrialCategory category)
        {
            var level = this.GetLevel();
            var levelSummary = level.GetSummary();
            var progress = level.GetProgress();

            switch (category)
            {
                case TimeTrialCategory.FairyPercent:
                {
                    int fairiesInLevel = levelSummary.TotalFairiesInLevel;
                    int fairiesFound = progress.CollectedFairies.Count;

                    return fairiesInLevel > 0 && fairiesFound >= fairiesInLevel;
                }

                case TimeTrialCategory.HundredPercent:
                {
                    int fairiesInLevel = levelSummary.TotalFairiesInLevel;
                    int fairiesFound = progress.CollectedFairies.Count;
                    bool allFairies = fairiesFound >= fairiesInLevel;

                    int gemsInLevel = levelSummary.TotalGemsInLevel;
                    int gemsFound = progress.TotalGemsCollected;
                    bool allGems = gemsFound >= gemsInLevel;

                    return allFairies && allGems;
                }

                default: return true;
            }
        }

        /// <summary>
        ///     Whether or not the given category even makes sense for this
        ///     level.  (IE: fairy percent doesn't make sense in levels without
        ///     any fairies)
        /// </summary>
        public bool IsRelevant(TimeTrialCategory category)
        {
            var level = this.GetLevel();
            var levelSummary = level.GetSummary();

            switch (category)
            {
                case TimeTrialCategory.AnyPercent: return true;

                case TimeTrialCategory.FairyPercent: return
                    levelSummary.TotalFairiesInLevel > 0;

                case TimeTrialCategory.HundredPercent: return
                    levelSummary.TotalFairiesInLevel > 0 &&
                    levelSummary.TotalGemsInLevel > 0;

                default: return false;
            }
        }

        public PhysicsTicks? TargetTime(TimeTrialCategory category)
        {
            return _targetTimes.TryGetValue(category, out PhysicsTicks value)
                ? value
                : null;
        }

        private void OnLevelReset()
        {
            if (!IsTimeTrialMode)
                return;

            if (_isResettingLevelProgress)
            {
                _isResettingLevelProgress = false;
                return;
            }

            Timer = 0;
            _targetTimes = CopyBestTimes();

            IsTimerRunning = false;
            EmitSignal(SignalName.ReadyToShowBriefing);

            // Respawn any collectables that may have been collected and reset
            // any flags that may have been set on the previous attempt.
            DummyProgress = new LevelProgress();
            SaveFileManager.Current.CurrentLevelVisit = new();

            // HACK: We don't technically know which order the LevelReset
            // handlers will run in.  Some gems may have already reset
            // themselves based on the previous level progress before we had
            // time to swap it.
            // So, let's fire the reset event one more time to ensure EVERYONE
            // sees the clean save file.
            _isResettingLevelProgress = true;
            SignalBus.Instance.EmitLevelReset();
        }

        private void OnExitReached()
        {
            if (IsTimeTrialMode)
            {
                Finish();
            }
        }

        public void ShowResultsScreen()
        {
            EmitSignal(SignalName.ReadyToShowResults);
        }

        public void Start()
        {
            GetTree().Paused = false;
            IsTimerRunning = true;

            EmitSignal(SignalName.TimeTrialStarted);
        }

        private void Finish()
        {
            IsTimerRunning = false;

            // Update the best time for all categories that the player has met
            // the requirements for
            foreach (var category in Enum.GetValues<TimeTrialCategory>())
            {
                if (!RequirementsMet(category))
                    continue;

                var targetTime = GetSavedBestTime(category) ?? PhysicsTicks.MaxValue;
                if (Timer < targetTime)
                    SetSavedBestTime(category, Timer);
            }

            SaveFileManager.Instance.RequestAutosave();

            EmitSignal(SignalName.TimeTrialFinished);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsTimerRunning && IsTimeTrialMode && !GetTree().Paused)
            {
                Timer++;
                // TODO: compensate for slo-mo effects?
            }
        }

        public PhysicsTicks? GetSavedBestTime(TimeTrialCategory category)
        {
            var saveData = CurrentLevelSaveData();
            return saveData.TimeTrialBestTime.TryGetValue(category, out var time)
                ? time
                : null;
        }

        private void SetSavedBestTime(TimeTrialCategory category, PhysicsTicks time)
        {
            var save = CurrentLevelSaveData();
            save.TimeTrialBestTime[category] = time;
        }

        private LevelSaveData CurrentLevelSaveData()
        {
            return SaveFileManager
                .Current
                .GetLevelSaveData(this.GetLevel().SceneFilePath);
        }

        private Dictionary<TimeTrialCategory, PhysicsTicks> CopyBestTimes()
        {
            return CurrentLevelSaveData()
                .TimeTrialBestTime
                .ToDictionary();
        }
    }
}