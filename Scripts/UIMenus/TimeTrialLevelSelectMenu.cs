using Godot;

namespace FastDragon
{
    public partial class TimeTrialLevelSelectMenu : Control
    {
        private PageNavigator _pageNav => GetNode<PageNavigator>("%PageNav");
        private Page _levelSelectPage => GetNode<Page>("%LevelSelectPage");
        private Page _categorySelectPage => GetNode<Page>("%CategorySelectPage");
        private Control _levelButtons => GetNode<Control>("%LevelButtons");

        private string _selectedMapFilePath;

        public override void _Ready()
        {
            foreach (string mapFilePath in AllUnlockedLevels())
            {
                string capturedFilePath = mapFilePath;  // Saving it for the closure
                var cacheEntry = AtlasCache.Instance.GetEntry(mapFilePath);

                var button = new Button();
                button.Text = cacheEntry.HumanReadableName;
                button.Pressed += () =>
                {
                    _selectedMapFilePath = capturedFilePath;
                    ShowCategorySelectPage();
                };
                _levelButtons.AddChild(button);
            }
            _levelSelectPage.FocusedControl = _levelButtons.GetChild<Control>(0);

            ShowLevelSelectPage();
        }

        public void OnBackPressed() => _pageNav.CurrentPage?.GoBack();

        public void ReturnToTitle() => MapTransitionManager.Instance.GoToTitleScreen();

        public void ShowLevelSelectPage()
        {
            _pageNav.ChangePage(_levelSelectPage);
        }

        public void ShowCategorySelectPage()
        {
            _pageNav.ChangePage(_categorySelectPage);

            // Enable/disable all the categories that have been locked/unlocked
            var entry = TimeTrialSaveData.Instance.GetEntry(_selectedMapFilePath);
            GetNode<Button>("%AnyPercentButton").Disabled = !entry.AnyPercentUnlocked;
            GetNode<Button>("%FairyPercentButton").Disabled = !entry.FairyPercentUnlocked;

            GD.Print($"AnyPercentUnlocked: {entry.AnyPercentUnlocked}");
            GD.Print($"FairyPercentUnlocked: {entry.FairyPercentUnlocked}");
        }

        public void StartAnyPercent()
        {
            Start(TimeTrialManager.TimeTrialMode.AnyPercent);
        }

        public void StartFairyPercent()
        {
            Start(TimeTrialManager.TimeTrialMode.FairyPercent);
        }

        private string[] AllUnlockedLevels()
        {
            return TimeTrialSaveData.Instance.UnlockedMaps;
        }

        private void Start(TimeTrialManager.TimeTrialMode mode)
        {
            MapTransitionManager.Instance.GoToMapForTimeTrial(
                _selectedMapFilePath,
                mode
            );
        }
    }
}