namespace FastDragon
{
    public struct PhysicsTicks
    {
        public uint Ticks;
        public double Seconds => Ticks * (1.0 / 60);

        public PhysicsTicks(uint ticks)
        {
            Ticks = ticks;
        }

        public static implicit operator uint(PhysicsTicks ticks) => ticks.Ticks;
        public static implicit operator PhysicsTicks(uint ticks) => new PhysicsTicks(ticks);
    }
}