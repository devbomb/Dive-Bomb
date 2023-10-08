using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class SignalBus : Node
    {
        public static SignalBus Instance {get; private set;}

        // Why am I doing this instead of using a Godot signal?
        // It's a workaround for this bug:
        // https://github.com/godotengine/godot/issues/82346
        //
        // tldr: If you connect a Godot signal in C#, and then the handler node
        // is QueueFree()'d(EG: because you changed maps), then the Callable
        // won't be disconnected from the signal.  That's a problem for us,
        // because SignalBus is a singleton that persists between map changes.
        public event Action LevelReset
        {
            add => _levelReset.Add(value);
            remove => _levelReset.Remove(value);
        }
        private FakeSignal<Action> _levelReset = new FakeSignal<Action>();

        public override void _Ready()
        {
            Instance = this;
        }

        public void EmitLevelReset() => _levelReset.Emit();

        private class FakeSignal<TDelegate> where TDelegate : Delegate
        {
            private List<TDelegate> _handlers = new List<TDelegate>();
            private List<TDelegate> _swap = new List<TDelegate>();

            public void Add(TDelegate handler) => _handlers.Add(handler);
            public void Remove(TDelegate handler) => _handlers.Remove(handler);

            public void Emit(params object[] args)
            {
                // Filter out deleted objects, without incurring any GC
                _swap.Clear();
                foreach (var handler in _handlers)
                {
                    if (handler.Target is GodotObject obj && !IsInstanceValid(obj))
                        continue;

                    _swap.Add(handler);
                }

                _handlers.Clear();
                _handlers.AddRange(_swap);

                // Execute the remaining handlers
                foreach (var handler in _handlers)
                {
                    handler.DynamicInvoke(args);
                }
            }
        }
    }
}