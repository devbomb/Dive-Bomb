using System;
using Newtonsoft.Json;

namespace FastDragon
{
    [JsonConverter(typeof(PhysicsTicksJsonConverter))]
    public struct PhysicsTicks
    {
        public uint Ticks;
        public double Seconds => Ticks * (1.0 / 60);

        public static PhysicsTicks MaxValue => uint.MaxValue;

        public PhysicsTicks(uint ticks)
        {
            Ticks = ticks;
        }

        public static implicit operator uint(PhysicsTicks ticks) => ticks.Ticks;
        public static implicit operator PhysicsTicks(uint ticks) => new PhysicsTicks(ticks);

        public override string ToString() => Ticks.ToString();
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