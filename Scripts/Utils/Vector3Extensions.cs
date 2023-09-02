using Godot;

namespace FastDragon
{
    public static class Vector3Extensions
    {
        public static Vector3 Flattened(this Vector3 v)
        {
            var result = v;
            result.Y = 0;
            return result;
        }

        public static Vector3 ForwardToEulerAnglesRad(this Vector3 forward)
        {
            if (forward.IsParallelTo(Vector3.Up))
            {
                if (forward.Y > 0)
                    return new Vector3(Mathf.DegToRad(90), 0, 0);
                else
                    return new Vector3(Mathf.DegToRad(-90), 0, 0);
            }

            return Transform3D.Identity
                .LookingAt(forward, Vector3.Up)
                .Basis
                .GetEuler();
        }

        public static bool IsParallelTo(this Vector3 v, Vector3 other)
        {
            return v.Cross(other) == Vector3.Zero;
        }

        public static Vector3 ProjectOnPlane(this Vector3 v, Vector3 planeNormal)
        {
            return new Plane(planeNormal).Project(v);
        }
    }
}