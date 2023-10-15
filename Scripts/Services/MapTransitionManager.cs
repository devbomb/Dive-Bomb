using Godot;

namespace FastDragon
{
    public partial class MapTransitionManager : Node
    {
        public static MapTransitionManager Instance {get; private set;}

        [Export(PropertyHint.File)] public string LevelSelectMap;
        [Export] public PackedScene PortalLoadingScreenPrefab;

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

        public void GoToPortalLoadingScreen(
            string levelSceneFile,
            Environment skyBoxEnvironment
        )
        {
            var tree = GetTree();
            var oldScene = tree.CurrentScene;
            var loadingScreen = PortalLoadingScreenPrefab.Instantiate<PortalLoadingScreen>();

            // TODO: Sync player values between oldScene and loadingScreen

            tree.Root.RemoveChild(oldScene);
            oldScene.QueueFree();

            tree.Root.AddChild(loadingScreen);
            tree.CurrentScene = loadingScreen;

            loadingScreen.Initialize(
                levelSceneFile,
                skyBoxEnvironment
            );
        }
    }
}