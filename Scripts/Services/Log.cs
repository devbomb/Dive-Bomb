
using System;
using Serilog;

using Godot;
using System.Reflection;

namespace FastDragon
{
    public partial class Log : Node
    {
        private static readonly Lazy<Serilog.Core.Logger> _logger = new(() =>
        {
            string sqliteFile = System.IO.Path.Combine(
                OS.GetUserDataDir(),
                "DiveBombLogs.sqlite"
            );

            string textFile = System.IO.Path.Combine(
                OS.GetUserDataDir(),
                "DiveBombLogs.log"
            );

            return new LoggerConfiguration()
                .WriteTo.SQLite(sqliteFile) // Log to SQLite for easy querying/statistics
                .WriteTo.File(textFile)     // Log to text for quick readability
                .CreateLogger();
        });

        public override void _EnterTree()
        {
            Log.GameStarted();
        }

        public static void GameStarted()
        {
            _logger.Value.Information("Game started");
        }
    }
}