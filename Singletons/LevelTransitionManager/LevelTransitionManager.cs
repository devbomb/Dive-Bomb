using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class LevelTransitionManager : Node
    {
        public static LevelTransitionManager Instance {get; private set;}

        [Export(PropertyHint.File)] public string TimeTrialLevelSelectScene;
        [Export(PropertyHint.File)] public string TitleScreenScene;

        [Export] public PackedScene PortalLoadingScreenPrefab;

        private ColorRect _fadeCurtain => GetNode<ColorRect>("%FadeCurtain");

        public override void _Ready()
        {
            Instance = this;
        }

        public void ChangeSceneToNode(Node scene)
        {
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

        public void GoToTimeTrialLevelSelect()
        {
            GetTree().ChangeSceneToFile(TimeTrialLevelSelectScene);
        }

        public void GoToTitleScreen()
        {
            DoThingWithFadeToBlack(() =>
            {
                Log.StartedGoToLevelWithFade(
                    GetTree().CurrentScene.Name,
                    TitleScreenScene,
                    true,
                    false
                );
                GetTree().ChangeSceneToFile(TitleScreenScene);
                Log.FinishedGoToLevelWithFade();
            });
        }

        public void GoToLevel(string levelSceneFile)
        {
            GetTree().ChangeSceneToFile(levelSceneFile);
        }

        public void GoToLevelWithFadeToBlack(string levelSceneFile)
        {
            DoThingWithFadeToBlack(() =>
            {
                Log.StartedGoToLevelWithFade(
                    GetTree().CurrentScene.Name,
                    levelSceneFile,
                    true,
                    false
                );
                GoToLevel(levelSceneFile);
                Log.FinishedGoToLevelWithFade();
            });
        }

        public void GoToLevelForTimeTrial(string levelSceneFile)
        {
            DoThingWithFadeToBlack(() =>
            {
                Log.StartedGoToLevelWithFade(
                    GetTree().CurrentScene.Name,
                    levelSceneFile,
                    true,
                    true
                );

                var levelRoot = ResourceLoader
                    .Load<PackedScene>(levelSceneFile)
                    .Instantiate<DiveBombLevel>();

                ChangeSceneToNode(levelRoot);

                Log.FinishedGoToLevelWithFade();

                levelRoot.TimeTrial.EnterTimeTrialMode();
            });
        }

        public void EnterLevel(
            string levelSceneFile,
            Environment skyBoxEnvironment
        )
        {
            GoToPortalLoadingScreen(levelSceneFile, null, skyBoxEnvironment);
            SaveFileManager.Current.CurrentCheckpoint = null;
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
            // The fade curtain blocks everything that isn't flagged as
            // "visible in portals", so setting that flag on the player ensures
            // he's still visible during the fadeout.
            player.SetVisibleInPortals(true);
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

            // HACK: Setting the process mode to Physics avoids a "black flash"
            // that would otherwise occur for one frame after switching to the
            // loading screen.
            //
            // Why does that flash happen without this, and why does changing
            // the process mode fix it?  Not sure, but it may have something to
            // do with CopycatCamera not moving to the loading screen camera's
            // position until _after_ a frame has already been drawn with it in
            // the wrong place.
            tween.SetProcessMode(Tween.TweenProcessMode.Physics);
        }

        public void ExitLevel()
        {
            var oldScene = GetTree().CurrentScene;

            // Use the previous level's skybox during the loading screen.
            // If the level doesn't have one, use a placeholder.
            Environment skyBoxEnvironment = oldScene.FindNode<WorldEnvironment>()?.Environment;
            if (skyBoxEnvironment == null)
            {
                skyBoxEnvironment = ResourceLoader.Load<Environment>("res://Environments/DaySky.tres");
            }

            string levelSceneFile = GetHomeWorldLevel();
            string previousLevelSceneFile = oldScene.SceneFilePath;
            GoToPortalLoadingScreen(levelSceneFile, previousLevelSceneFile, skyBoxEnvironment);
            SaveFileManager.Current.CurrentCheckpoint = null;
        }

        public void RespawnPlayerAfterDeath()
        {
            // Heal the player back to full
            SaveFileManager.Current.PlayerHealth = Player.MaxHealth;

            // Fade to black, reset the level, and then unfade.
            DoThingWithFadeToBlack(SignalBus.Instance.EmitLevelReset);
        }

        private void DoThingWithFadeToBlack(System.Action action)
        {
            const double fadeOutTime = 0.5;
            const double pauseTime = 0.25;
            const double fadeInTime = 0.5;

            // Heal the player back to full
            SaveFileManager.Current.PlayerHealth = Player.MaxHealth;

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

            // After everything has faded to black, do the thing
            tween.TweenCallback(Callable.From(action));
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
            string previousLevelSceneFile,
            Environment skyBoxEnvironment
        )
        {
            var parameters = LoadingScreenParameters.FromCurrentLevel(
                levelSceneFile,
                previousLevelSceneFile,
                skyBoxEnvironment,
                GetTree()
            );

            var loadingScreen = PortalLoadingScreenPrefab.Instantiate<PortalLoadingScreen>();
            ChangeSceneToNode(loadingScreen);
            loadingScreen.Initialize(parameters);
        }

        private string GetHomeWorldLevel()
        {
            return GetTree().FindNode<DiveBombLevel>()?.HomeWorldLevel;
        }
    }
}