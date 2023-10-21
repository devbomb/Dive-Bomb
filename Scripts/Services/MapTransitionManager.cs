using System.Linq;
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

        public void ChangeSceneToNode(Node scene)
        {
            var tree = GetTree();
            var oldScene = tree.CurrentScene;

            // Swap the current scene out for the loading screen
            tree.Root.RemoveChild(oldScene);
            oldScene.QueueFree();

            tree.Root.AddChild(scene);
            tree.CurrentScene = scene;
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

            // Save player/camera values so they can be copied over to the
            // loading screen, creating the illusion of a seamless transition
            var oldPlayer = oldScene
                .EnumerateDescendantsOfType<Player>()
                .First();

            double animationStartTime = oldPlayer.Animator.CurrentAnimationPosition;
            Vector3 playerRotRad = oldPlayer.GlobalRotation;
            float cameraDist = oldPlayer.Camera.OrbitDistance;
            float cameraYawRad = oldPlayer.Camera.OrbitYawRad;
            float cameraPitchRad = oldPlayer.Camera.OrbitPitchRad;

            ChangeSceneToNode(loadingScreen);

            loadingScreen.Initialize(
                levelSceneFile,
                skyBoxEnvironment,
                animationStartTime,
                playerRotRad,
                cameraDist,
                cameraYawRad,
                cameraPitchRad
            );
        }
    }
}