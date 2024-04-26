using Godot;

namespace FastDragon
{
    public partial class TitleScreen : Control
    {
        [Export(PropertyHint.File)] public string NewGameMap;

        public void NewGame()
        {
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