using Godot;

namespace FastDragon
{
    public static class PhysicsInterpolationExtensions
    {
        public static void ResetPhysicsInterpolation(this Node3D node)
        {
            PhysicsInterpolatorSingleton.Instance.ResetPhysicsInterpolation(node);
        }

        public static bool IsPhysicsInterpolated(this Node3D node)
        {
            return node.IsInGroup(PhysicsInterpolatorSingleton.GroupName);
        }

        public static void SetPhysicsInterpolated(this Node3D node, bool value)
        {
            if (value)
                node.AddToGroup(PhysicsInterpolatorSingleton.GroupName);
            else
                node.RemoveFromGroup(PhysicsInterpolatorSingleton.GroupName);
        }
    }
}