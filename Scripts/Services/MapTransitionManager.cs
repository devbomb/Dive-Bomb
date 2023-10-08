using Godot;

namespace FastDragon
{
    public partial class MapTransitionManager : Node
    {
        public static MapTransitionManager Instance {get; private set;}

        [Export(PropertyHint.File)] public string LevelSelectMap;

        public override void _Ready()
        {
            Instance = this;
        }

        public void GoToLevelSelect()
        {
            GetTree().ChangeSceneToFile(LevelSelectMap);
        }

        public void GoToMap(string mapSceneFile)
        {
            GetTree().ChangeSceneToFile(mapSceneFile);
        }
    }
}