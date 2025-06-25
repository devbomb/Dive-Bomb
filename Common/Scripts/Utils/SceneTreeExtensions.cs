using System.Linq;
using Godot;

namespace FastDragon
{
    public static class SceneTreeExtensions
    {
        /// <summary>
        /// Returns the first(in depth-first-search order) descendant node of
        /// the given type, or null if none exists.
        /// </summary>
        /// <param name="node"></param>
        /// <typeparam name="TNode"></typeparam>
        /// <returns></returns>
        public static TNode FindNode<TNode>(this SceneTree tree) where TNode : Node
        {
            return tree.Root.FindNode<TNode>();
        }
    }
}