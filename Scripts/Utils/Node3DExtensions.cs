using Godot;
namespace FastDragon
{
    public static class Node3DExtensions
    {
        /// <summary>
        /// Returns a unit vector pointing in this node's "forward" direction
        /// in global space
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static Vector3 GlobalForward(this Node3D node)
        {
            return -node.GlobalTransform.Basis.Z;
        }
    }
}