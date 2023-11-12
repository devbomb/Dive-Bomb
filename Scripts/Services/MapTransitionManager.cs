using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class MapTransitionManager : Node
    {
        public static MapTransitionManager Instance {get; private set;}

        [Export(PropertyHint.File)] public string LevelSelectMap;
        [Export] public PackedScene PortalLoadingScreenPrefab;

        private ColorRect _fadeCurtain => GetNode<ColorRect>("%FadeCurtain");

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

        public void EnterLevel(
            string levelSceneFile,
            Environment skyBoxEnvironment
        )
        {
            GoToPortalLoadingScreen(levelSceneFile, null, skyBoxEnvironment);
        }

        public void ExitLevelFromPauseMenu()
        {
            const double fadeOutTime = 0.375;
            const double fadeInTime = 0.375;

            var player = GetTree().FindNode<Player>();
            var fadeCurtain = GetNode<NonPlayerFadeCurtain>("%NonPlayerFadeCurtain");

            // Pause the scene (but not the whole game!) during the fadeout,
            // to avoid shenanigans
            GetTree().CurrentScene.ProcessMode = ProcessModeEnum.Disabled;
            player.Animator.ProcessMode = ProcessModeEnum.Always;

            // Transition the player to a gliding animation
            player.ChangeState<PlayerManhandledState>();
            player.Animator.Play("Glide", fadeOutTime);

            // Fade the screen to black(except for the player)
            fadeCurtain.Visible = true;
            fadeCurtain.FadePercent = 0;
            var tween = GetTree().CreateTween();
            tween.TweenProperty(fadeCurtain, "FadePercent", 1, fadeOutTime);

            // After everything has faded to black, go to the loading screen
            tween.TweenCallback(Callable.From(ExitLevel));

            // After going to the loading screen, start fading the screen back
            // in.  This will still look seamless because the player is
            // excluded from the fade-out.
            tween.TweenProperty(fadeCurtain, "FadePercent", 0, fadeInTime);
            tween.TweenProperty(fadeCurtain, "visible", false, 0);
        }

        public void ExitLevel()
        {
            var oldScene = GetTree().CurrentScene;

            // Use the previous map's skybox during the loading screen.
            // If the level doesn't have one, use a placeholder.
            Environment skyBoxEnvironment = oldScene.FindNode<WorldEnvironment>()?.Environment;
            if (skyBoxEnvironment == null)
            {
                skyBoxEnvironment = ResourceLoader.Load<Environment>("res://Environments/DaySky.tres");
            }

            // Find the worldspawn and ask it which homeworld we should go to.
            // If there is no homeworld assigned, go to the level select menu
            // instead.
            var worldSpawn = GetTree().FindNode<WorldSpawn>();
            if (worldSpawn?.HomeWorld == null)
            {
                GoToLevelSelect();
                return;
            }

            string levelSceneFile = worldSpawn.HomeWorld;
            string previousMapFile = oldScene.SceneFilePath;
            GoToPortalLoadingScreen(levelSceneFile, previousMapFile, skyBoxEnvironment);
        }

        public void RespawnPlayerAfterDeath()
        {
            const double fadeOutTime = 0.5;
            const double pauseTime = 0.25;
            const double fadeInTime = 0.5;

            // Pause the scene (but not the whole game!) during the fadeout,
            // to avoid shenanigans
            GetTree().CurrentScene.ProcessMode = ProcessModeEnum.Disabled;

            // Fade the screen to black
            var tween = GetTree().CreateTween();

            _fadeCurtain.Color = Colors.Black;
            _fadeCurtain.Modulate = Colors.Transparent;
            tween.TweenProperty(
                _fadeCurtain,
                "modulate",
                Colors.White,
                fadeOutTime
            );
            tween.TweenInterval(pauseTime);

            // After everything has faded to black, reset the level
            tween.TweenCallback(Callable.From(SignalBus.Instance.EmitLevelReset));
            tween.TweenProperty(
                _fadeCurtain,
                "modulate",
                Colors.Transparent,
                fadeInTime
            );

            // After everything is faded back in, unpause
            tween.TweenCallback(Callable.From(() =>
            {
                GetTree().CurrentScene.ProcessMode = ProcessModeEnum.Inherit;
            }));
        }

        private void GoToPortalLoadingScreen(
            string levelSceneFile,
            string previousMapFile,
            Environment skyBoxEnvironment
        )
        {
            var oldScene = GetTree().CurrentScene;
            var loadingScreen = PortalLoadingScreenPrefab.Instantiate<PortalLoadingScreen>();

            // Save player/camera values so they can be copied over to the
            // loading screen, creating the illusion of a seamless transition
            var oldPlayer = oldScene.FindNode<Player>();

            double animationStartTime = oldPlayer.Animator.CurrentAnimationPosition;
            Vector3 playerRotRad = oldPlayer.GlobalRotation;
            float cameraDist = oldPlayer.Camera.OrbitDistance;
            float cameraYawRad = oldPlayer.Camera.OrbitYawRad;
            float cameraPitchRad = oldPlayer.Camera.OrbitPitchRad;

            // Save the sun, too, so there isn't a jarring lighting change
            var sun = oldScene.FindNode<DirectionalLight3D>();
            sun.GetParent().RemoveChild(sun);

            ChangeSceneToNode(loadingScreen);

            loadingScreen.Initialize(
                levelSceneFile,
                previousMapFile,
                skyBoxEnvironment,
                animationStartTime,
                playerRotRad,
                cameraDist,
                cameraYawRad,
                cameraPitchRad,
                sun
            );
        }
    }
}