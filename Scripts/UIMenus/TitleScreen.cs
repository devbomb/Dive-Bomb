using Godot;

namespace FastDragon
{
    public partial class TitleScreen : Control
    {
        [Export(PropertyHint.File)] public string NewGameMap;

        private Control _buttons => GetNode<Control>("%Buttons");

        public override void _Ready()
        {
            _buttons.GetChild<Button>(0).GrabFocus();
        }

        public void NewGame()
        {
            SaveFile.Current = new SaveFile();
            MapTransitionManager.Instance.GoToMap(NewGameMap);
        }

        public void Continue()
        {
            // TODO: Ask the player which save file to load
            string fileName = "user://Saves/Slot0.json";
            if (!FileAccess.FileExists(fileName))
                return;

            using var file = FileAccess.Open(fileName, FileAccess.ModeFlags.Read);
            string json = file.GetAsText();
            file.Close();

            SaveFile.Current = SaveFile.FromJson(json);
            MapTransitionManager.Instance.GoToMap(SaveFile.Current.CurrentMap);
        }
    }
}