using System;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialManager : Node
    {
        public bool IsTimeTrialMode => Mode != null;
        public bool IsTimerRunning {get; private set;} = false;

        public TimeTrialCategory? Mode {get; private set;} = null;

        public uint TimerPhysicsTicks {get; private set;}
        public uint TargetTimePhysicsTicks {get; private set;}

        public LevelProgress DummyProgress { get; private set; } = new();

        [Signal] public delegate void ReadyToShowBriefingEventHandler();
        [Signal] public delegate void TimeTrialStartedEventHandler();
        [Signal] public delegate void TimeTrialFinishedEventHandler();
        [Signal] public delegate void ReadyToShowResultsEventHandler();

        private bool _isResettingLevelProgress = false;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += OnLevelReset;
            SignalBus.Instance.ExitReached += OnExitReached;
            ProcessMode = ProcessModeEnum.Always;
        }

        public void EnterTimeTrialMode(TimeTrialCategory mode)
        {
            Mode = mode;
            SaveFileManager.Current.CurrentCheckpoint = null;
            SignalBus.Instance.EmitLevelReset();
        }

        public void ExitTimeTrialMode()
        {
            Mode = null;
            SignalBus.Instance.EmitLevelReset();
        }

        public bool RequirementsMet()
        {
            switch (Mode)
            {
                case TimeTrialCategory.FairyPercent:
                {
                    var level = this.GetLevel();
                    var levelSummary = AtlasCache.Instance.GetEntry(level.SceneFilePath);
                    int fairiesFound = level.GetProgress().CollectedFairies.Count;

                    return fairiesFound >= levelSummary.TotalFairiesInLevel;
                }

                default: return true;
            }
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
            TargetTimePhysicsTicks = GetSavedBestTime();

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

            if (TimerPhysicsTicks < GetSavedBestTime())
                SetSavedBestTime(TimerPhysicsTicks);

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

        private uint GetSavedBestTime()
        {
            var entry = CurrentCategoryEntry();

            return entry.BestTimePhysicsTicks == null
                ? uint.MaxValue
                : entry.BestTimePhysicsTicks.Value;
        }

        private void SetSavedBestTime(uint timePhysicsTicks)
        {
            CurrentCategoryEntry().BestTimePhysicsTicks = timePhysicsTicks;
            TimeTrialSaveData.Instance.SaveToJson();
        }

        private TimeTrialSaveData.CategoryEntry CurrentCategoryEntry()
        {
            return TimeTrialSaveData
                .Instance
                .GetEntry(SaveFileManager.Current.CurrentLevel, Mode.Value);
        }
    }
}