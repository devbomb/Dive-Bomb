using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PauseMenu : PageNavigator
    {
        private bool _open = false;
        private Control _buttons => GetNode<Control>("%Buttons");
        private Control _timeTrialButtons => GetNode<Control>("%TimeTrialButtons");

        private Page _mainPage => GetNode<Page>("%MainPage");
        private Page _timeTrialMainPage => GetNode<Page>("%TimeTrialMainPage");
        private Page _atlasPage => GetNode<Page>("%AtlasMenu");
        private UserSettingsMenu _userSettingsMenu => GetNode<UserSettingsMenu>("%UserSettingsMenu");

        private Button _exitLevelButton => GetNode<Button>("%ExitLevel");

        public override void _Input(InputEvent ev)
        {
            if (InputService.PauseJustPressed(ev))
            {
                if (_open)
                    Close();
                else
                    Open();
            }
        }

        public override void _Ready()
        {
            Close();
            _exitLevelButton.Visible = this.GetLevel()?.HomeWorldLevel != null;
        }

        public void Open()
        {
            // Do not allow the pause menu to be opened while the game is
            // already paused for a different reason(EG: a fadeout transition)
            if (GetTree().Paused)
                return;

            _open = true;
            Visible = true;
            GetTree().Paused = true;

            OpenMainPage();
        }

        public void Close()
        {
            _open = false;
            Visible = false;
            GetTree().Paused = false;
        }

        public void OpenMainPage()
        {
            if (this.GetLevel()?.TimeTrial?.IsTimeTrialMode ?? false)
            {
                ChangePage(_timeTrialMainPage);
                _timeTrialButtons.GetChild<Button>(0).GrabFocus();
            }
            else
            {
                ChangePage(_mainPage);
                _buttons.GetChild<Button>(0).GrabFocus();
            }
        }

        public void OpenAtlas()
        {
            ChangePage(_atlasPage);
        }

        public void OpenUserSettingsMenu()
        {
            ChangePage(_userSettingsMenu);
        }

        public void OpenDebugMenu()
        {
            ChangePage(GetNode<Page>("%DebugMenu"));
        }

        public void ReturnToCheckpoint()
        {
            Close();
            LevelTransitionManager.Instance.RespawnPlayerAfterDeath();
        }

        public void SaveGame()
        {
            SaveFileManager.Instance.SaveToSlot(SaveFileManager.Instance.ActiveSlot);
        }

        public void FullyResetLevel()
        {
            Close();
            this.GetLevel()?.GetProgress().ResetProgress();
            SaveFileManager.Current.CurrentCheckpoint = null;
            LevelTransitionManager.Instance.RespawnPlayerAfterDeath();
        }

        public void ExitLevel()
        {
            Close();
            LevelTransitionManager.Instance.ExitLevelFromPauseMenu();
        }

        public void QuitToTitle()
        {
            Close();
            LevelTransitionManager.Instance.GoToTitleScreen();
        }

        public void EnterTimeTrialAnyPercent()
        {
            Close();
            this.GetLevel()?.TimeTrial.EnterTimeTrialMode(TimeTrialCategory.AnyPercent);
        }

        public void EnterTimeTrialFairyPercent()
        {
            Close();
            this.GetLevel()?.TimeTrial.EnterTimeTrialMode(TimeTrialCategory.FairyPercent);
        }

        public void ExitTimeTrialMode()
        {
            Close();
            this.GetLevel()?.TimeTrial.ExitTimeTrialMode();
        }
    }
}