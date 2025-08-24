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
        public static Godot.Collections.Array<Node3D> GetOverlappingBodies(
            this Area3D area,
            int safetyTimer
        )
        {
            return Area3DSafetyTimer.Instance.GetOverlappingBodies(
                area,
                safetyTimer
            );
        }
    }
}