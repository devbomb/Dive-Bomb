using Godot;
using System;
using System.Collections.Generic;

namespace FastDragon
{
    public class Coroutine
    {
        public bool Done { get; private set; }
        private readonly IEnumerator<YieldInstruction> _executor;
        private YieldInstruction _currentInstruction = default;

        public Coroutine(IEnumerator<YieldInstruction> executor)
        {
            _executor = executor;
        }

        public void Tick(double delta)
        {
            if (Done)
                return;

            if (!Engine.IsInPhysicsFrame() && _currentInstruction.Type == YieldInstruction.InstructionType.WaitTicks)
            {
                throw new Exception("Coroutine.WaitTicks is only valid if your Tick() call is inide _PhysicsProcess()");
            }

            _currentInstruction.SecondsToWait -= delta;
            _currentInstruction.TicksToWait--;
            _currentInstruction.ChildRunner?.Tick(delta);

            if (!_currentInstruction.Done)
                return;

            bool result = _executor.MoveNext();
            _currentInstruction = _executor.Current;

            Done = !result || _currentInstruction.Type == YieldInstruction.InstructionType.Stop;
        }

        public static YieldInstruction WaitSeconds(double seconds) => new()
        {
            Type = YieldInstruction.InstructionType.WaitSeconds,
            SecondsToWait = seconds
        };

        public static YieldInstruction WaitTicks(PhysicsTicks ticks) => new()
        {
            Type = YieldInstruction.InstructionType.WaitTicks,
            TicksToWait = ticks
        };

        public static YieldInstruction WaitFor(IEnumerator<YieldInstruction> coroutine) => new()
        {
            Type = YieldInstruction.InstructionType.WaitChildCoroutine,
            ChildRunner = new Coroutine(coroutine)
        };

        public static YieldInstruction Stop() => new()
        {
            Type = YieldInstruction.InstructionType.Stop
        };
    }

    public struct YieldInstruction
    {
        public bool Done => Type switch
        {
            InstructionType.None => true,
            InstructionType.WaitSeconds => SecondsToWait <= 0,
            InstructionType.WaitTicks => TicksToWait <= 0,
            InstructionType.WaitChildCoroutine => ChildRunner.Done,
            InstructionType.Stop => true,
            _ => throw new ArgumentException()
        };

        public InstructionType Type = InstructionType.None;
        public enum InstructionType
        {
            None,
            Stop,
            WaitSeconds,
            WaitTicks,
            WaitChildCoroutine,
        }
        public double SecondsToWait = 0;
        public PhysicsTicks TicksToWait = 0;
        public Coroutine ChildRunner = null;

        public YieldInstruction() {}
    }
}