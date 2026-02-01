using System;
using System.Linq;

using Godot;

namespace FastDragon
{
    public interface IPowerable
    {
        string Id { get; }

        /// <summary>
        ///     Tells the node that it is now being powered (or not).
        /// </summary>
        void SetPowered(bool powered);

        /// <summary>
        ///     Tells the node that it is now being powered (or not), and that
        ///     it should skip any "transitional" states and go straight to the
        ///     final one.
        ///
        ///     IE: If this is a door, then "ForceSetPowered(true)" will make
        ///     it go from "closed -> open" instead of "closed -> opening -> open".
        ///
        ///     Use this when resetting the level or loading a save file to make
        ///     it seem like the node was already in the powered state.
        /// </summary>
        void ForceSetPowered(bool powered) => SetPowered(powered);
    }

    public static class IPowerableNodeExtensions
    {
        public static IPowerable FindPowerable(this Node node, string id)
        {
            return node.GetTree()
                .Root
                .EnumerateDescendantsOfType<IPowerable>()
                .Single(p => p.Id == id);
        }
    }
}