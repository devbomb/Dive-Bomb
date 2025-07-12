using Godot;

namespace FastDragon
{
    public static class Vector3Extensions
    {
        public static Vector3 Flattened(this Vector3 v, Vector3? upDirection = null)
        {
            Vector3 up = upDirection ?? Vector3.Up;
            return v.ProjectOnPlane(up);
        }

        public static Vector3 ForwardToEulerAnglesRad(
            this Vector3 forward,
            Vector3? upDirection = null
        )
        {
            Vector3 up = upDirection ?? Vector3.Up;

            if (forward.IsParallelTo(up))
            {
                if (forward.ComponentAlong(up) > 0)
                    return new Vector3(Mathf.DegToRad(90), 0, 0);
                else
                    return new Vector3(Mathf.DegToRad(-90), 0, 0);
            }

            return Transform3D.Identity
                .LookingAt(forward, up)
                .Basis
                .GetEuler();
        }

        /// <summary>
        /// Assumes an euler order of YXZ
        /// </summary>
        /// <param name="eulerAnglesRad"></param>
        /// <param name="order"></param>
        /// <returns></returns>
        public static Vector3 EulerAnglesRadToForward(this Vector3 eulerAnglesRad)
        {
            return Vector3.Forward
                .Rotated(Vector3.Up, eulerAnglesRad.Y)
                .Rotated(Vector3.Right, eulerAnglesRad.X)
                .Rotated(Vector3.Forward, eulerAnglesRad.Z);
        }

        public static Vector3 LerpEulerRad(
            this Vector3 fromRad,
            Vector3 toRad,
            float t
        )
        {
            return new Vector3(
                Mathf.LerpAngle(fromRad.X, toRad.X, t),
                Mathf.LerpAngle(fromRad.Y, toRad.Y, t),
                Mathf.LerpAngle(fromRad.Z, toRad.Z, t)
            );
        }

        public static Vector3 LerpEulerRadSinusoidal(
            this Vector3 fromRad,
            Vector3 toRad,
            float t
        )
        {
            return new Vector3(
                MathUtils.LerpAngleSinusoidal(fromRad.X, toRad.X, t),
                MathUtils.LerpAngleSinusoidal(fromRad.Y, toRad.Y, t),
                MathUtils.LerpAngleSinusoidal(fromRad.Z, toRad.Z, t)
            );
        }

        public static Vector3 LerpBezier(
            this Vector3 from,
            Vector3 to,
            Vector3 control,
            float t
        )
        {
            var a = from.Lerp(control, t);
            var b = from.Lerp(to, t);
            return a.Lerp(b, t);
        }

        public static Vector3 LerpParabola(
            this Vector3 from,
            Vector3 to,
            float height,
            float t
        )
        {
            float x = (2 * t) - 1;
            float y = 1f - (x * x);
            y *= height;

            var result = from.Lerp(to, t);
            result.Y += y;

            return result;
        }

        public static Vector3 RotateTowardEulerRad(
            this Vector3 fromRad,
            Vector3 toRad,
            float deltaRad
        )
        {
            return new Vector3(
                AngleMath.MoveToward(fromRad.X, toRad.X, deltaRad),
                AngleMath.MoveToward(fromRad.Y, toRad.Y, deltaRad),
                AngleMath.MoveToward(fromRad.Z, toRad.Z, deltaRad)
            );
        }

        public static Vector3 DecayTowardsEulerRad(
            this Vector3 fromRad,
            Vector3 toRad,
            float decayRate,
            float delta
        )
        {
            return new Vector3(
                AngleMath.DecayToward(fromRad.X, toRad.X, decayRate, delta),
                AngleMath.DecayToward(fromRad.Y, toRad.Y, decayRate, delta),
                AngleMath.DecayToward(fromRad.Z, toRad.Z, decayRate, delta)
            );
        }

        public static Vector3 DecayToward(
            this Vector3 from,
            Vector3 to,
            float decayRate,
            float delta
        )
        {
            float remaining = from.DistanceTo(to);
            remaining *= Mathf.Pow(Mathf.E, -decayRate * delta);
            return to.MoveToward(from, remaining);
        }

        public static bool IsParallelTo(this Vector3 v, Vector3 other)
        {
            return v.Cross(other) == Vector3.Zero;
        }

        public static float ComponentAlong(this Vector3 v, Vector3 other)
        {
            return v.Dot(other) / other.Length();
        }

        public static Vector3 ProjectOnPlane(this Vector3 v, Vector3 planeNormal)
        {
            return new Plane(planeNormal).Project(v);
        }
    }
}