using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    [JsonObject(MemberSerialization.OptIn)]
    public partial class UserSettings : RefCounted
    {
        private const string FilePath = "user://UserSettings.json";

        public static UserSettings Instance { get; } = LoadFromJson();

        [JsonProperty] public bool ShowPerformanceStats = false;
        [JsonProperty] public bool UsePhysicsInterpolation = true;

        [JsonProperty] public bool InvertCameraX = false;
        [JsonProperty] public bool InvertCameraY = false;
        [JsonProperty] public float CameraSensController = 1;
        [JsonProperty] public float CameraSensMouse = 1;

        [JsonProperty] public float MasterVolumeLinear
        {
            get => GetBusVolumeLinear("Master");
            set => SetBusVolumeLinear("Master", value);
        }
        [JsonProperty] public float SfxVolumeLinear
        {
            get => GetBusVolumeLinear("Sfx");
            set => SetBusVolumeLinear("Sfx", value);
        }
        [JsonProperty] public float MusicVolumeLinear
        {
            get => GetBusVolumeLinear("Music");
            set => SetBusVolumeLinear("Music", value);
        }
        [JsonProperty] public float DialogVoiceVolumeLinear
        {
            get => GetBusVolumeLinear("DialogVoice");
            set => SetBusVolumeLinear("DialogVoice", value);
        }

        public void SaveToJson()
        {
            string json = JsonConvert.SerializeObject(
                this,
                new JsonSerializerSettings { Formatting = Formatting.Indented }
            );

            using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Write);
            file.StoreLine(json);
            file.Close();
        }

        private void SetBusVolumeLinear(string bus, float volumeLinear)
        {
            int busIndex = AudioServer.GetBusIndex(bus);
            AudioServer.SetBusVolumeLinear(busIndex, volumeLinear);
        }

        private float GetBusVolumeLinear(string bus)
        {
            int busIndex = AudioServer.GetBusIndex(bus);
            return AudioServer.GetBusVolumeLinear(busIndex);
        }

        private static UserSettings LoadFromJson()
        {
            if (!FileAccess.FileExists(FilePath))
                return new UserSettings();

            try
            {
                using var file = FileAccess.Open(FilePath, FileAccess.ModeFlags.Read);
                string json = file.GetAsText();
                file.Close();

                return JsonConvert.DeserializeObject<UserSettings>(json);
            }
            catch (JsonException err)
            {
                GD.PushWarning($"Error parsing UserSettings.json.  Using default settings.\n{err}");
                return new UserSettings();
            }
        }
    }
}