using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class LevelManifest : Resource
    {
        [Export] public string HumanReadableName { get; set; }
        [Export(PropertyHint.FilePath)] public string SkyBoxEnvironmentFilePath { get; set; }
            = "res://Common/Skyboxes/DigitalWorld/DigitalWorldSkybox.tres";
        [Export(PropertyHint.FilePath)] public string SceneFilePath { get; set; }
    }
}