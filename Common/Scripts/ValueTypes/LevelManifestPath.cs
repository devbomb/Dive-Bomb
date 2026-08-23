using System;
using System.Text.Json.Serialization;
using Godot;

namespace FastDragon
{
    [JsonConverter(typeof(LevelManifestPathJsonConverter))]
    public record struct LevelManifestPath(string Path)
    {
        public LevelManifest Manifest => ResourceLoader.Load<LevelManifest>(Path);

        public static implicit operator LevelManifest(LevelManifestPath path)
        {
            return path.Manifest;
        }

        public static implicit operator LevelManifestPath(LevelManifest level)
        {
            return new(level.ResourcePath);
        }
    }

    public class LevelManifestPathJsonConverter : JsonConverter<LevelManifestPath>
    {
        public override LevelManifestPath Read(
            ref System.Text.Json.Utf8JsonReader reader,
            Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options
        )
        {
            return new(reader.GetString());
        }

        public override void Write(
            System.Text.Json.Utf8JsonWriter writer,
            LevelManifestPath value,
            System.Text.Json.JsonSerializerOptions options
        )
        {
            writer.WriteStringValue(value.Path);
        }
    }
}