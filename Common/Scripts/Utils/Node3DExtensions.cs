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

        public static Transform3D GetGlobalTransformOutsideOfTree(this Node3D node)
        {
            var parent = node.GetParentOrNull<Node3D>();
            if (parent == null)
            {
                return node.Transform;
            }

            return node.Transform * GetGlobalTransformOutsideOfTree(parent);
        }
    }
}