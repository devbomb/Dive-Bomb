using Godot;

namespace FastDragon
{
    public struct SphereCoords
    {
        public float YawRad;
        public float PitchRad;
        public float Distance;

        public SphereCoords() {}

        public SphereCoords(float yawRad, float pitchRad, float distance)
        {
            YawRad = yawRad;
            PitchRad = pitchRad;
            Distance = distance;
        }

        public SphereCoords Lerp(SphereCoords to, float t)
        {
            return new()
            {
                YawRad = Mathf.LerpAngle(YawRad, to.YawRad, t),
                PitchRad = Mathf.LerpAngle(PitchRad, to.PitchRad, t),
                Distance = Mathf.Lerp(Distance, to.Distance, t),
            };
        }

        public Vector3 ToCartesian()
        {
            Vector3 dir = Vector3.Back
                .Rotated(Vector3.Right, PitchRad)
                .Rotated(Vector3.Up, YawRad);

            return dir * Distance;
        }
    }
}