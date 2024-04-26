using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class MapTransitionManager : Node
    {
        public static MapTransitionManager Instance {get; private set;}

        public bool CurrentMapIsHomeWorld => string.IsNullOrEmpty(GetHomeWorldMap());

        [Export(PropertyHint.File)] public string LevelSelectMap;
        [Export(PropertyHint.File)] public string TitleScreenMap;

        [Export] public PackedScene PortalLoadingScreenPrefab;

        private ColorRect _fadeCurtain => GetNode<ColorRect>("%FadeCurtain");

        public override void _Ready()
        {
            Instance = this;
            SaveFile.Current.CurrentMap = GetTree().CurrentScene.SceneFilePath;
        }

        public string GetHomeWorldMap()
        {
            var worldSpawn = GetTree().FindNode<WorldSpawn>();
            return worldSpawn?.HomeWorld;
        }

        public void ChangeSceneToNode(Node scene)
        {
            SaveFile.Current.CurrentMap = scene.SceneFilePath;

            var tree = GetTree();

            // Unload the old scene
            // UnloadCurrentScene() causes nondeterministic crashes,
            // so we need to do it "manually" instead.
            // See this issue: https://github.com/godotengine/godot/issues/85692
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

        public void GoToTitleScreen()
        {
            GetTree().ChangeSceneToFile(TitleScreenMap);
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
            SaveFile.Current.CurrentCheckpoint = null;
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
            tween.TweenInterval(1);
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

            string levelSceneFile = GetHomeWorldMap();
            string previousMapFile = oldScene.SceneFilePath;
            GoToPortalLoadingScreen(levelSceneFile, previousMapFile, skyBoxEnvironment);
            SaveFile.Current.CurrentCheckpoint = null;
        }

        public void RespawnPlayerAfterDeath()
        {
            const double fadeOutTime = 0.5;
            const double pauseTime = 0.25;
            const double fadeInTime = 0.5;

            // Heal the player back to full
            SaveFile.Current.PlayerHealth = SparxColor.Gold;

            var tween = GetTree().CreateTween();
            tween.SetPauseMode(Tween.TweenPauseMode.Process);
            tween.TweenCallback(Callable.From(() =>
            {
                // Pause the game during the fadeout, to avoid shenanigans.
                //
                // HACK: I _wanted_ to just set CurrentScene's ProcessMode to
                // "Disabled" instead of pausing the whole game, but that apparently
                // causes any Area3Ds the player is colliding with to stop detecting
                // collisions.  Pausing the whole game doesn't cause that problem,
                // though.
                // Thanks, Godot >.<
                //
                // HACK: This needs to be done as part of the tween, instead of
                // _before_ the tween, to avoid a weird camera flicker effect.
                // Why?  I don't know.
                GetTree().Paused = true;
            }));

            // Fade the screen to black
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
                GetTree().Paused = false;
            }));
        }

        private void GoToPortalLoadingScreen(
            string levelSceneFile,
            string previousMapFile,
            Environment skyBoxEnvironment
        )
        {
            var parameters = LoadingScreenParameters.FromCurrentMap(
                levelSceneFile,
                previousMapFile,
                skyBoxEnvironment,
                GetTree()
            );

            var loadingScreen = PortalLoadingScreenPrefab.Instantiate<PortalLoadingScreen>();
            ChangeSceneToNode(loadingScreen);
            loadingScreen.Initialize(parameters);
        }
    }
}