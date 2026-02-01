using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public static class NodeExtensions
    {
        /// <summary>
        /// A garbage-collector-friendly alternative to <see cref="Node.GetChildren"/>.
        ///
        /// If used directly in a foreach loop, no garbage will be created when
        /// this method is called.
        ///
        /// <see cref="Node.GetChildren"/> causes stuttering in the browser if
        /// you call it every frame, because it allocates the output array on
        /// the heap.  These arrays pile up until they force the garbage
        /// collector to run.
        ///
        /// <see cref="NodeExtensions.EnumerateChildren"/>, on the other hand,
        /// only allocates a tiny enumerator struct.  If that struct is used in
        /// a foreach loop(and not anywhere else), the compiler will put that
        /// struct on the stack instead of the heap, meaning no garbage is
        /// created.
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static IEnumerable<Node> EnumerateChildren(this Node node)
        {
            int count = node.GetChildCount();
            for (int i = 0; i < count; i++)
            {
                yield return (Node)(node.GetChild(i));
            }
        }

        public static IEnumerable<Node> EnumerateDescendants(this Node node)
        {
            foreach (var d in Recursive(node))
                yield return d;

            IEnumerable<Node> Recursive(Node n)
            {
                foreach (var child in n.EnumerateChildren())
                {
                    var childNode = (Node)child;
                    yield return childNode;

                    foreach (var d in Recursive(childNode))
                        yield return d;
                }
            }
        }

        public static IEnumerable<TNode> EnumerateDescendantsOfType<TNode>(this Node node)
        {
            foreach (var d in node.EnumerateDescendants())
            {
                if (d is TNode target)
                    yield return target;
            }
        }

        /// <summary>
        /// Finds the first ancestor(or self) of the given type.
        /// Returns null if there are no ancestors(or self) of said type.
        /// </summary>
        /// <typeparam name="TNode"></typeparam>
        public static TNode FirstAncestor<TNode>(this Node node) where TNode : class
        {
            if (node == null)
                return null;

            if (node is TNode n)
                return n;

            return node.GetParent().FirstAncestor<TNode>();
        }

        /// <summary>
        /// Returns the first(in depth-first-search order) descendant node of
        /// the given type, or null if none exists.
        /// </summary>
        /// <param name="node"></param>
        /// <typeparam name="TNode"></typeparam>
        /// <returns></returns>
        public static TNode FindNode<TNode>(this Node node) where TNode : Node
        {
            return node.EnumerateDescendantsOfType<TNode>().FirstOrDefault();
        }

        public static bool IsAncestorInGroup(this Node node, StringName groupName)
        {
            if (node.IsInGroup(groupName))
                return true;

            if (node.GetParent() is Node parent)
                return parent.IsAncestorInGroup(groupName);

            return false;
        }
    }
}
