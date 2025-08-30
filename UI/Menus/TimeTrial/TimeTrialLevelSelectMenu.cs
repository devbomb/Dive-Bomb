using System;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialLevelSelectMenu : Control
    {
        private PageNavigator _pageNav => GetNode<PageNavigator>("%PageNav");
        private Page _levelSelectPage => GetNode<Page>("%LevelSelectPage");
        private Page _categorySelectPage => GetNode<Page>("%CategorySelectPage");
        private Control _levelButtons => GetNode<Control>("%LevelButtons");
        private Control _categoryButtons => GetNode<Control>("%CategoryButtons");

        private string _selectedLevelScenePath;

        public override void _Ready()
        {
            foreach (string levelScenePath in AllUnlockedLevels())
            {
                string capturedFilePath = levelScenePath;  // Saving it for the closure
                var cacheEntry = AtlasCache.Instance.GetEntry(levelScenePath);

                var button = new Button();
                button.Text = cacheEntry.HumanReadableName;
                button.Pressed += () =>
                {
                    _selectedLevelScenePath = capturedFilePath;
                    ShowCategorySelectPage();
                };
                _levelButtons.AddChild(button);
            }
            _levelSelectPage.FocusedControl = _levelButtons.GetChild<Control>(0);

            ShowLevelSelectPage();
        }

        public void OnBackPressed() => _pageNav.CurrentPage?.GoBack();

        public void ReturnToTitle() => LevelTransitionManager.Instance.GoToTitleScreen();

        public void ShowLevelSelectPage()
        {
            _pageNav.ChangePage(_levelSelectPage);
        }

        public void ShowCategorySelectPage()
        {
            _pageNav.ChangePage(_categorySelectPage);

            // Enable/disable all the categories that have been locked/unlocked
            var saveData = TimeTrialSaveData.Instance;
            foreach (var category in Enum.GetValues<TimeTrialCategory>())
            {
                bool unlocked = saveData.GetEntry(_selectedLevelScenePath, category).Unlocked;
                _categoryButtons.GetNode<Button>(category.ToString()).Disabled = !unlocked;
            }
        }

        public void StartAnyPercent()
        {
            Start(TimeTrialCategory.AnyPercent);
        }

        public void StartFairyPercent()
        {
            Start(TimeTrialCategory.FairyPercent);
        }

        private string[] AllUnlockedLevels()
        {
            return TimeTrialSaveData.Instance.UnlockedLevels;
        }

        private void Start(TimeTrialCategory mode)
        {
            LevelTransitionManager.Instance.GoToLevelForTimeTrial(
                _selectedLevelScenePath,
                mode
            );
        }
    }
}