using Newtonsoft.Json;
using Godot;

namespace FastDragon
{
    public partial class UserSettings : Resource
    {
        private const string FilePath = "user://UserSettings.json";

        public static UserSettings Instance { get; } = LoadFromJson();

        public bool ShowPerformanceStats = false;
        public bool UsePhysicsInterpolation = true;

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