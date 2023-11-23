using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class PauseMenu : Control
    {
        private bool _open = false;
        private Control _buttons => GetNode<Control>("%Buttons");

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

            _buttons.GetChild<Button>(0).GrabFocus();
        }

        public void Close()
        {
            _open = false;
            Visible = false;
            GetTree().Paused = false;
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

        public void LoadSaveFile()
        {
            // TODO: Ask the player which save file to load
            string fileName = "user://Saves/Slot0.json";
            if (!FileAccess.FileExists(fileName))
                return;

            using var file = FileAccess.Open(fileName, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            file.Close();

            SaveFile.Current = SaveFile.FromJson(json);
            Close();
            MapTransitionManager.Instance.GoToMap(SaveFile.Current.CurrentMap);
        }

        public void ExitLevel()
        {
            Close();
            MapTransitionManager.Instance.ExitLevelFromPauseMenu();
        }
    }
}