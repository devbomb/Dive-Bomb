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

        public uint TimerPhysicsTicks {get; private set;}

        public LevelProgress DummyProgress { get; private set; } = new();

        [Signal] public delegate void ReadyToShowBriefingEventHandler();
        [Signal] public delegate void TimeTrialStartedEventHandler();
        [Signal] public delegate void TimeTrialFinishedEventHandler();
        [Signal] public delegate void ReadyToShowResultsEventHandler();

        private bool _isResettingLevelProgress = false;

        private Dictionary<TimeTrialCategory, uint> _targetTimes = new();

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += OnLevelReset;
            SignalBus.Instance.ExitReached += OnExitReached;
            ProcessMode = ProcessModeEnum.Always;
        }

        public void EnterTimeTrialMode()
        {
            IsTimeTrialMode = true;

            _targetTimes = CopyBestTimes();
            SaveFileManager.Current.CurrentCheckpoint = null;
            SignalBus.Instance.EmitLevelReset();
        }

        public void ExitTimeTrialMode()
        {
            IsTimeTrialMode = false;
            IsTimerRunning = false;
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

        public uint TargetTimePhysicsTicks(TimeTrialCategory category)
        {
            return _targetTimes.TryGetValue(category, out uint value)
                ? value
                : uint.MaxValue;
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

            TimerPhysicsTicks = 0;
            _targetTimes = CopyBestTimes();

            IsTimerRunning = false;
            EmitSignal(SignalName.ReadyToShowBriefing);

            // Respawn any collectables that may have been collected on the
            // previous attempt.
            DummyProgress = new LevelProgress();

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

                uint targetTimePhysicsTicks = GetSavedBestTime(category) ?? uint.MaxValue;
                if (TimerPhysicsTicks < targetTimePhysicsTicks)
                    SetSavedBestTime(category, TimerPhysicsTicks);
            }

            EmitSignal(SignalName.TimeTrialFinished);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsTimerRunning && IsTimeTrialMode && !GetTree().Paused)
            {
                TimerPhysicsTicks++;
                // TODO: compensate for slo-mo effects?
            }
        }

        public uint? GetSavedBestTime(TimeTrialCategory category)
        {
            var saveData = CurrentLevelSaveData();
            return saveData.TimeTrialBestTimePhysicsTicks.TryGetValue(category, out var time)
                ? time
                : null;
        }

        private void SetSavedBestTime(TimeTrialCategory category, uint timePhysicsTicks)
        {
            var save = CurrentLevelSaveData();
            save.TimeTrialBestTimePhysicsTicks[category] = timePhysicsTicks;
        }

        private LevelSaveData CurrentLevelSaveData()
        {
            return SaveFileManager
                .Current
                .GetLevelSaveData(this.GetLevel().SceneFilePath);
        }

        private Dictionary<TimeTrialCategory, uint> CopyBestTimes()
        {
            return CurrentLevelSaveData()
                .TimeTrialBestTimePhysicsTicks
                .ToDictionary();
        }
    }
}