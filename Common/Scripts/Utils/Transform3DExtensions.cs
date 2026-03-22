using Godot;

namespace FastDragon
{
    public static class Transform3DExtensions
    {
        public static Transform3D WithOnlyYRotation(this Transform3D transform)
        {
            float yRotRad = transform.Basis.GetEuler().Y;

            return Transform3D.Identity
                .Rotated(Vector3.Up, yRotRad)
                .Translated(transform.Origin);
        }

        public static Transform3D WithOrigin(this Transform3D transform, Vector3 origin)
        {
            return Transform3D.Identity.Translated(-transform.Origin + origin);
        }

        public static Transform3D DecayToward(
            this Transform3D from,
            Transform3D to,
            float decayRate,
            float delta
        )
        {
            float t = MathUtils.DecayToward(1, 0, decayRate, delta);
            return from.InterpolateWith(to, t);
        }
    }
}