
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
                Log.Information("Game started {InitialScene}", GetTree().CurrentScene.Name);
            }).CallDeferred();
        }

        public static void Information(string template, params object[] values)
        {
            _logger.Value.Information(template, values);
        }
    }
}