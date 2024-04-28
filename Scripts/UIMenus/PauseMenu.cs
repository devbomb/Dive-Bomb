using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PauseMenu : PageNavigator
    {
        private bool _open = false;
        private Control _buttons => GetNode<Control>("%Buttons");

        private Page _mainPage => GetNode<Page>("%MainPage");
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
            _exitLevelButton.Visible = !MapTransitionManager.Instance.CurrentMapIsHomeWorld;
        }

        public void Open()
        {
            // Do not allow the pause menu to be opened while the game is
            // already paused for a different reason(EG: a fadeout transition)
            if (GetTree().Paused)
                return;

            ChangePage(_mainPage);

            _open = true;
            Visible = true;
            GetTree().Paused = true;

            _buttons.GetChild<Button>(0).GrabFocus();
        }

        public void Close()
        {
            _open = false;
            Visible = false;
            GetTree().Paused = false;
        }

        public void OpenAtlas()
        {
            ChangePage(_atlasPage);
        }

        public void OpenUserSettingsMenu()
        {
            ChangePage(_userSettingsMenu);
        }

        public void ResetLevel()
        {
            Close();
            MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        }

        public void SaveGame()
        {
            // TODO: Ask the player which save file to overwrite
            DirAccess.MakeDirRecursiveAbsolute("user://Saves");
            using var file = FileAccess.Open("user://Saves/Slot0.json", FileAccess.ModeFlags.Write);
            file.StoreLine(SaveFile.Current.ToJson());
            file.Close();
        }

        public void ExitLevel()
        {
            Close();
            MapTransitionManager.Instance.ExitLevelFromPauseMenu();
        }

        public void QuitToTitle()
        {
            Close();
            MapTransitionManager.Instance.GoToTitleScreen();
        }
    }
}