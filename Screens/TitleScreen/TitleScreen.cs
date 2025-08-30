using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class TitleScreen : Control
    {
        private const string Slot0Path = "user://Saves/Slot0.json";

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
            SaveFile.Current = new SaveFile();
            LevelTransitionManager.Instance.GoToLevelWithFadeToBlack(NewGameLevel);
        }

        public void Continue()
        {
            // TODO: Ask the player which save file to load

            using var file = FileAccess.Open(Slot0Path, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            file.Close();

            SaveFile.Current = SaveFile.FromJson(json);
            LevelTransitionManager.Instance.GoToLevelWithFadeToBlack(SaveFile.Current.CurrentLevel);
        }

        public void TimeTrialMode()
        {
            LevelTransitionManager.Instance.GoToTimeTrialLevelSelect();
        }

        public void OpenMainPage()
        {
            _pageNav.ChangePage(GetNode<Page>("%MainPage"));

            _continueButton.Visible = FileAccess.FileExists(Slot0Path);

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