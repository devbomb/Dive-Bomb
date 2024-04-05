using Godot;

namespace FastDragon
{
    public static class PhysicsInterpolationExtensions
    {
        public static void ResetPhysicsInterpolation(this Node3D node)
        {
            PhysicsInterpolatorSingleton.Instance.ResetPhysicsInterpolation(node);
        }
    }
}