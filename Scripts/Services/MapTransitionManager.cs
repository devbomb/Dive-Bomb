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

            string portalID = worldSpawn.PortalID;

            // TODO: go to a loading screen, instead of going directly to the
            // home world
            var scenePrefab = ResourceLoader.Load<PackedScene>(worldSpawn.HomeWorld);
            var node = scenePrefab.Instantiate<Node3D>();
            ChangeSceneToNode(node);

            var portal = node.EnumerateDescendantsOfType<Portal>()
                .First(p => p.PortalID == portalID);

            portal.PlayExitAnimation();
        }
    }
}