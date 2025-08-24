using System;
using Godot;

namespace FastDragon
{
    public partial class Area3DSafetyTimer : Node
    {
        public static Area3DSafetyTimer Instance { get; private set; }

        private int _physicsFramesSinceReset;
        private readonly Godot.Collections.Array<Node3D> _empty;

        public Area3DSafetyTimer()
        {
            _empty = new Godot.Collections.Array<Node3D>();
            _empty.MakeReadOnly();
        }

        public override void _Ready()
        {
            Instance = this;
            SignalBus.Instance.LevelReset += Reset;
            ProcessPhysicsPriority = int.MaxValue;

            _empty.MakeReadOnly();
            Reset();
        }

        private void Reset()
        {
            _physicsFramesSinceReset = 0;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_physicsFramesSinceReset < int.MaxValue)
                _physicsFramesSinceReset++;
        }

        public Godot.Collections.Array<Node3D> GetOverlappingBodies(
            Area3D area,
            int safetyTimer
        )
        {
            if (_physicsFramesSinceReset < safetyTimer)
                return _empty;

            return area.GetOverlappingBodies();
        }
    }

    public static class Area3DSafetyTimerExtensions
    {
        /// <summary>
        ///     Like Godot's GetOverlappingBodies(), but it returns an empty
        ///     array if it's been too soon since the last level reset.
        ///
        ///     This avoids some egregious false positives that can happen
        ///     if a body gets moved outside of an Area3D during a level reset.
        ///
        ///     For whatever reason, updates to an Area3D's overlapping bodies
        ///     are delayed by a few physics frames.  Waiting for a few frames
        ///     ensures the overlapping bodies list has had enough time to
        ///     update.
        /// </summary>
        public static Godot.Collections.Array<Node3D> GetOverlappingBodiesResetSafe(
            this Area3D area,
            int safetyTimer = 2
        )
        {
            return Area3DSafetyTimer.Instance.GetOverlappingBodies(
                area,
                safetyTimer
            );
        }
    }
}