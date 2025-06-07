
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
    }
}