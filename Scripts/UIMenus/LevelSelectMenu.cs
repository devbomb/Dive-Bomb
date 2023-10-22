using Godot;

namespace FastDragon
{
    public partial class LevelSelectMenu : Control
    {
        private const string MapsFolder = "res://Scenes/Maps/Levels";
        private Control _buttonsContainer => GetNode<Control>("%LevelButtons");

        public override void _Ready()
        {
            string[] mapSceneFiles = DirAccess.GetFilesAt(MapsFolder);

            foreach (string file in mapSceneFiles)
            {
                var button = new Button();
                button.Text = file;
                button.Pressed += () =>
                {
                    string filePath = $"{MapsFolder}/{file}";
                    MapTransitionManager.Instance.GoToMap(filePath);
                };

                _buttonsContainer.AddChild(button);
            }
        }
    }
}