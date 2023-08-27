using Godot;

namespace FastDragon
{
    public static class Vector3Extensions
    {
        public static Vector3 Flattened(this Vector3 v)
        {
            var result = v;
            v.Y = 0;
            return result;
        }
    }
}