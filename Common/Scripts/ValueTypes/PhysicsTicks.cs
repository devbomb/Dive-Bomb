using System;
using Godot;
using Newtonsoft.Json;

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
        public override bool CanRead => true;
        public override bool CanWrite => true;

        public override PhysicsTicks ReadJson(
            JsonReader reader,
            Type objectType,
            PhysicsTicks existingValue,
            bool hasExistingValue,
            JsonSerializer serializer
        )
        {
            return new PhysicsTicks((uint)Convert.ToInt32(reader.Value));
        }

        public override void WriteJson(JsonWriter writer, PhysicsTicks value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Ticks);
        }
    }
}