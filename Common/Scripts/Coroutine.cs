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

            _currentInstruction.SecondsToWait -= delta;
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
            WaitChildCoroutine,
        }
        public double SecondsToWait = 0;
        public Coroutine ChildRunner = null;

        public YieldInstruction() {}
    }
}