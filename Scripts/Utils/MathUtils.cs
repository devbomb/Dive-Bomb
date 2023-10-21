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

        public static float LerpSinusoidal(
            float from,
            float to,
            float t
        )
        {
            float shiftedT = -Mathf.Cos(t * Mathf.DegToRad(180)) + 1;
            shiftedT /= 2;

            return Mathf.Lerp(from, to, shiftedT);
        }

        public static float LerpAngleSinusoidal(
            float fromRad,
            float toRad,
            float t
        )
        {
            float shiftedT = -Mathf.Cos(t * Mathf.DegToRad(180)) + 1;
            shiftedT /= 2;

            return Mathf.LerpAngle(fromRad, toRad, shiftedT);
        }
    }
}