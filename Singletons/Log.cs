
using System;
using Serilog;

using Godot;
using System.Reflection;
using Serilog.Formatting.Compact;

namespace FastDragon
{
    public partial class Log : Node
    {
        private static readonly Lazy<Serilog.Core.Logger> _logger = new(() =>
        {
            string textFile = System.IO.Path.Combine(
                OS.GetUserDataDir(),
                "DiveBombLogs.log"
            );

            return new LoggerConfiguration()
                .WriteTo.File(new CompactJsonFormatter(), textFile)
                .CreateLogger();
        });

        public override void _EnterTree()
        {
            Callable.From(() =>
            {
                Log.GameStarted(GetTree().CurrentScene.Name);
            }).CallDeferred();
        }

        public static void GameStarted(string initialScene)
        {
            _logger.Value.Information("Game started {InitialScene}", initialScene);
        }

        public static void LoadingScreenStarted(
            string previousScene,
            string nextScene,
            bool returningHome,
            int totalTreasure,
            int gemsSpent
        )
        {
            string template = "Loading screen started"
                + " {PreviousScene}"
                + " {NextScene}"
                + " {ReturningHome}"
                + " {TotalTreasure}"
                + " {GemsSpent}";

            _logger.Value.Information(
                template,
                previousScene,
                nextScene,
                returningHome,
                totalTreasure,
                gemsSpent
            );
        }

        public static void LoadingScreenSkipped(string state)
        {
            _logger.Value.Information("Loading screen skipped {State}", state);
        }

        public static void LoadingScreenSoftlocked(double duration)
        {
            _logger.Value.Error(
                "Infinite loading screen (probably) {Duration}",
                duration
            );
        }

        public static void LoadingScreenFinished()
        {
            _logger.Value.Information("Loading screen finished");
        }

        public static void StartedGoToSceneWithFade(
            string previousScene,
            string nextScene,
            bool fadedToBlack,
            bool forTimeTrial
        )
        {
            string template = "Started going to a non-level scene with a fade"
                + " {PreviousScene}"
                + " {NextScene}"
                + " {FadedToBlack}"
                + " {ForTimeTrial}";

            _logger.Value.Information(
                template,
                previousScene,
                nextScene,
                fadedToBlack,
                forTimeTrial
            );
        }

        public static void FinishedGoToSceneWithFade()
        {
            _logger.Value.Information("Finished going to a non-level scene with a fade");
        }

        public static void StartedGoToLevelWithFade(
            string previousScene,
            string nextScene,
            bool fadedToBlack,
            bool forTimeTrial
        )
        {
            string template = "Started going to a level with a fade"
                + " {PreviousScene}"
                + " {NextScene}"
                + " {FadedToBlack}"
                + " {ForTimeTrial}";

            _logger.Value.Information(
                template,
                previousScene,
                nextScene,
                fadedToBlack,
                forTimeTrial
            );
        }

        public static void FinishedGoToLevelWithFade()
        {
            _logger.Value.Information("Finished going to a level with a fade");
        }
    }
}