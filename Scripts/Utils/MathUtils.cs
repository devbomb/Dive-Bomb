using Godot;
namespace FastDragon
{
    public static class MathUtils
    {
        public static float DecayToward(
            float from,
            float to,
            float decayRate,
            float delta
        )
        {
            float remaining = Mathf.Abs(from - to);
            remaining *= Mathf.Pow(Mathf.E, -decayRate * delta);
            return Mathf.MoveToward(to, from, remaining);
        }

        public static Vector3 DecayToward(
            Vector3 from,
            Vector3 to,
            float decayRate,
            float delta
        )
        {
            float remaining = from.DistanceTo(to);
            remaining *= Mathf.Pow(Mathf.E, -decayRate * delta);
            return to.MoveToward(from, remaining);
        }
    }
}