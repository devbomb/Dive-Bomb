using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class LevelManifest : Resource
    {
        /// <summary>
        ///     Indicates that this level is a debug level.
        ///     Debug levels are not meant to be accessible in the published
        ///     game.
        ///
        ///     Debug levels should not be included in any UI elements that
        ///     list all level in the game.
        /// </summary>
        [Export] public bool Debug { get; set; }
        [Export] public bool IsHubWorld { get; set; }

        [Export] public string HumanReadableName { get; set; }
        [Export(PropertyHint.FilePath)] public string SkyBoxEnvironmentFilePath { get; set; }
            = "res://Common/Skyboxes/DigitalWorld/DigitalWorldSkybox.tres";
        [Export(PropertyHint.FilePath)] public string SceneFilePath { get; set; }
    }
}