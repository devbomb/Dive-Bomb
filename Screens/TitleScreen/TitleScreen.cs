using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TitleScreen : Control
    {
        [Export(PropertyHint.File)] public string NewGameLevel;

        private Control _buttons => GetNode<Control>("%Buttons");
        private Button _continueButton => GetNode<Button>("%Continue");

        private PageNavigator _pageNav => GetNode<PageNavigator>("%PageNavigator");

        public override void _Ready()
        {
            OpenMainPage();
        }

        public void NewGame()
        {
            SaveFileManager.Instance.StartNewGame(0, NewGameLevel);
        }

        public void Continue()
        {
            // TODO: Ask the player which save file to load
            SaveFileManager.Instance.LoadFromSlot(0);
        }

        public void TimeTrialMode()
        {
            LevelTransitionManager.Instance.GoToTimeTrialLevelSelect();
        }

        public void OpenMainPage()
        {
            _pageNav.ChangePage(GetNode<Page>("%MainPage"));

            _continueButton.Visible = SaveFileManager.Instance.SlotHasData(0);

            _buttons.EnumerateChildren()
                .Cast<Button>()
                .First(b => b.Visible)
                .GrabFocus();
        }

        public void OpenUserSettingsMenu()
        {
            _pageNav.ChangePage(GetNode<Page>("%UserSettingsMenu"));
        }
    }
}