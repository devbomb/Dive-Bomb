using System;
using Godot;

namespace FastDragon
{
        public partial class UserSettings : RefCounted
    {
        private const string FilePath = "user://UserSettings.json";

        public static UserSettings Instance { get; } = LoadFromJson();

        public bool ShowPerformanceStats = false;
        public bool ShowPlayerVelocityStats = false;
        public bool UsePhysicsInterpolation = true;

        public bool InvertCameraX = false;
        public bool InvertCameraY = false;
        public float CameraSensController = 1;
        public float CameraSensMouse = 1;

        public float MasterVolumeLinear
        {
            get => GetBusVolumeLinear("Master");
            set => SetBusVolumeLinear("Master", value);
        }
        public float SfxVolumeLinear
        {
            get => GetBusVolumeLinear("Sfx");
            set => SetBusVolumeLinear("Sfx", value);
        }
        public float MusicVolumeLinear
        {
            get => GetBusVolumeLinear("Music");
            set => SetBusVolumeLinear("Music", value);
        }
        public float DialogVoiceVolumeLinear
        {
            get => GetBusVolumeLinear("DialogVoice");
            set => SetBusVolumeLinear("DialogVoice", value);
        }

        public void SaveToJson()
        {
            string json = JsonUtils.ToJson(this);

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

                return JsonUtils.FromJson<UserSettings>(json);
            }
            catch (Exception err)
            {
                GD.PushWarning($"Error parsing UserSettings.json.  Using default settings.\n{err}");
                return new UserSettings();
            }
        }
    }
}