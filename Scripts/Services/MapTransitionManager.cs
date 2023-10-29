using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class MapTransitionManager : Node
    {
        public static MapTransitionManager Instance {get; private set;}

        [Export(PropertyHint.File)] public string LevelSelectMap;
        [Export] public PackedScene PortalLoadingScreenPrefab;
        [Export] public PackedScene ReturnHomeLoadingScreenPrefab;

        public override void _Ready()
        {
            Instance = this;
            SaveFile.Current.CurrentMap = GetTree().CurrentScene.SceneFilePath;
        }

        public void ChangeSceneToNode(Node scene)
        {
            SaveFile.Current.CurrentMap = scene.SceneFilePath;

            var tree = GetTree();

            // Unload the old scene
            var oldScene = tree.CurrentScene;
            tree.Root.RemoveChild(oldScene);
            oldScene.QueueFree();

            // Change to the new one
            tree.Root.AddChild(scene);
            tree.CurrentScene = scene;
        }

        public void GoToLevelSelect()
        {
            SaveFile.Current.CurrentMap = LevelSelectMap;
            GetTree().ChangeSceneToFile(LevelSelectMap);
        }

        public void GoToMap(string mapSceneFile)
        {
            SaveFile.Current.CurrentMap = mapSceneFile;
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

        public void ExitLevel()
        {
            var worldSpawn = GetTree().Root
                .EnumerateDescendantsOfType<WorldSpawn>()
                .FirstOrDefault();

            // HACK: If this map does not have a home world assigned, go
            // straight to level select
            if (worldSpawn?.HomeWorld == null)
            {
                GoToLevelSelect();
                return;
            }

            string levelSceneFile = worldSpawn.HomeWorld;
            string previousMapFile = GetTree().CurrentScene.SceneFilePath;

            var tree = GetTree();
            var oldScene = tree.CurrentScene;
            var loadingScreen = ReturnHomeLoadingScreenPrefab.Instantiate<ReturnHomeLoadingScreen>();

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

            Environment skyBoxEnvironment = oldScene.FindNode<WorldEnvironment>()?.Environment;
            if (skyBoxEnvironment == null)
            {
                // Use a placeholder skybox, in case this level doesn't have
                // a WorldEnvironment.
                skyBoxEnvironment = ResourceLoader.Load<Environment>("res://Environments/DaySky.tres");
            }

            ChangeSceneToNode(loadingScreen);

            loadingScreen.Initialize(
                levelSceneFile,
                previousMapFile,
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