using System;
using System.Linq;

using Godot;

namespace FastDragon
{
    public interface IPowerable
    {
        string targetname { get; }

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
        /// <summary>
        ///     Synonym for "SetPowered(true)".
        ///     Exists mainly to establish the convention that "powered == open"
        /// </summary>
        public static void StartOpening(this IPowerable p) => p.SetPowered(true);

        /// <summary>
        ///     Synonym for "SetPowered(false)".
        ///     Exists mainly to establish the convention that "powered == open"
        /// </summary>
        public static void StartClosing(this IPowerable p) => p.SetPowered(false);

        /// <summary>
        ///     Synonym for "ForceSetPowered(true)".
        ///     Exists mainly to establish the convention that "powered == open"
        /// </summary>
        public static void InstantOpen(this IPowerable p) => p.ForceSetPowered(true);

        /// <summary>
        ///     Synonym for "ForceSetPowered(false)".
        ///     Exists mainly to establish the convention that "powered == open"
        /// </summary>
        public static void InstantClose(this IPowerable p) => p.ForceSetPowered(false);
    }
}