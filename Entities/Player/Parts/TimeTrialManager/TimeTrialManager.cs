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

        private Label _timerLabel => GetNode<Label>("%TimerLabel");
        private AnimationPlayer _timeAnnouncementAnimator => GetNode<AnimationPlayer>("%TimeAnnouncementAnimator");

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
                    var levelEntry = AtlasCache.Instance.GetEntry(saveFile.CurrentLevel);
                    int fairiesFound = saveFile.CurrentLevelProgress.CollectedFairies.Count;

                    return fairiesFound >= levelEntry.TotalFairiesInLevel;
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

            TimerPhysicsTicks = 0;
            TargetTimePhysicsTicks = GetSavedBestTime();

            IsTimerRunning = false;
            _pageNav.ChangePage(_briefingPage);

            // Reset the save file, to respawn any collectables that may have
            // been collected on the previous attempt.
            string currentLevel = SaveFile.Current.CurrentLevel;
            SaveFile.Current = new SaveFile();
            SaveFile.Current.CurrentLevel = currentLevel;

            // HACK: We don't technically know which order the LevelReset
            // handlers will run in.  Some gems may have already reset
            // themselves based on the previous save file before we had time to
            // swap it.  So, let's fire the reset event one more time to ensure
            // EVERYONE sees the clean save file.
            _isResettingSaveFile = true;
            SignalBus.Instance.EmitLevelReset();
        }

        private void OnExitReached()
        {
            if (IsTimeTrialMode)
            {
                Finish();
                return;
            }

            // Unlock time trial modes
            // TODO: Only do this if currently NOT in time trial mode
            string currentLevel = SaveFile.Current.CurrentLevel;
            var levelProgress = SaveFile.Current.CurrentLevelProgress;
            var atlasEntry = AtlasCache.Instance.GetEntry(currentLevel);

            bool levelHasGems = atlasEntry.TotalGemsInLevel > 0;
            bool levelHasFairies = atlasEntry.TotalFairiesInLevel > 0;

            TimeTrialSaveData.Instance.UnlockCategory(currentLevel, TimeTrialCategory.AnyPercent);

            if (levelHasFairies && levelProgress.FairiesCollected >= atlasEntry.TotalFairiesInLevel)
                TimeTrialSaveData.Instance.UnlockCategory(currentLevel, TimeTrialCategory.FairyPercent);
        }

        public void ShowResultsScreen()
        {
            _pageNav.ChangePage(_resultsPage);
        }

        public void Start()
        {
            _pageNav.ChangePage(null);
            IsTimerRunning = true;
            GetTree().Paused = false;
        }

        private void Finish()
        {
            IsTimerRunning = false;
            _timeAnnouncementAnimator.Play("TIME");

            if (TimerPhysicsTicks < GetSavedBestTime())
                SetSavedBestTime(TimerPhysicsTicks);
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsTimerRunning && IsTimeTrialMode && !GetTree().Paused)
            {
                TimerPhysicsTicks++;
                // TODO: compensate for slo-mo effects?
            }

            _timerLabel.Visible = IsTimeTrialMode;
            _timerLabel.Text = TimeUtils.FormatPhysicsTicksStopwatch(TimerPhysicsTicks);
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
                .GetEntry(SaveFile.Current.CurrentLevel, Mode.Value);
        }
    }
}