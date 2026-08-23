using System;
using System.Text.Json.Serialization;
using Godot;

namespace FastDragon
{
    [JsonConverter(typeof(PhysicsTicksJsonConverter))]
    public struct PhysicsTicks
    {
        public uint Ticks;
        public double Seconds => ((double)Ticks) / Engine.PhysicsTicksPerSecond;

        public static PhysicsTicks MaxValue => uint.MaxValue;

        public PhysicsTicks(uint ticks)
        {
            Ticks = ticks;
        }

        public static implicit operator uint(PhysicsTicks ticks) => ticks.Ticks;
        public static implicit operator PhysicsTicks(uint ticks) => new PhysicsTicks(ticks);

        public override string ToString() => Ticks.ToString();

        public string FormatStopwatch()
        {
            return TimeSpan.FromSeconds(Seconds).ToString(@"mm\:ss\.ff");
        }

        public string FormatStopwatchWithHours()
        {
            return TimeSpan.FromSeconds(Seconds).ToString(@"hh\:mm\:ss\.ff");
        }
    }

    public class PhysicsTicksJsonConverter : JsonConverter<PhysicsTicks>
    {
        public override PhysicsTicks Read(
            ref System.Text.Json.Utf8JsonReader reader,
            Type typeToConvert,
            System.Text.Json.JsonSerializerOptions options
        )
        {
            return reader.GetUInt32();
        }

        public override void Write(
            System.Text.Json.Utf8JsonWriter writer,
            PhysicsTicks value,
            System.Text.Json.JsonSerializerOptions options
        )
        {
            writer.WriteNumberValue(value);
        }
    }
}