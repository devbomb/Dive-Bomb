using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PauseMenu : PageNavigator
    {
        [Export] public AudioStreamPlayer OpenSound;
        [Export] public AudioStreamPlayer CloseSound;

        public bool IsOpen => _open;

        private bool _open = false;
        private Control _buttons => GetNode<Control>("%Buttons");
        private Control _timeTrialButtons => GetNode<Control>("%TimeTrialButtons");

        private Page _mainPage => GetNode<Page>("%MainPage");
        private Page _timeTrialMainPage => GetNode<Page>("%TimeTrialMainPage");
        private Page _atlasPage => GetNode<Page>("%AtlasMenu");
        private UserSettingsMenu _userSettingsMenu => GetNode<UserSettingsMenu>("%UserSettingsMenu");

        private Button _exitLevelButton => GetNode<Button>("%ExitLevel");
        private Button _quitToTitleButton => GetNode<Button>("%QuitToTitle");

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
            _quitToTitleButton.Visible = this.GetLevel()?.IsHomeWorld ?? true;
            _exitLevelButton.Visible = !_quitToTitleButton.Visible;
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

            CloseSound.Stop();
            OpenSound.Play();
        }

        public void Close()
        {
            OpenSound.Stop();

            // HACK: don't play the sound when the level initially loads
            if (_open)
                CloseSound.Play();

            _open = false;
            Visible = false;
            GetTree().Paused = false;
        }

        public void OpenMainPage()
        {
            if (this.IsTimeTrialMode())
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
            LevelTransitionManager.Instance.ReloadCheckpoint();
        }

        public void FullyResetLevel()
        {
            Close();

            this.GetLevel()?.GetProgress().ResetProgress();
            SaveFileManager.Current.CurrentCheckpoint = null;
            SaveFileManager.Current.CurrentLevelVisit = new();

            LevelTransitionManager.Instance.ReloadCheckpoint();
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

        public void EnterTimeTrialMode()
        {
            Close();
            this.GetLevel()?.TimeTrial.EnterTimeTrialMode();
        }
    }
}