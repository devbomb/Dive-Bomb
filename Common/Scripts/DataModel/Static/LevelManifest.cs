using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class LevelManifest : Resource
    {
        private const string DefaultSkyboxPath = "res://Common/Skyboxes/DigitalWorld/DigitalWorldSkybox.tres";
        [Export] public string LevelId { get; set; }
        [Export] public string HumanReadableName { get; set; }
        [Export] public Environment SkyBoxEnvironment { get; set; }
            = ResourceLoader.Load<Environment>(DefaultSkyboxPath);
        [Export(PropertyHint.FilePath)] public string SceneFilePath { get; set; }
    }
}