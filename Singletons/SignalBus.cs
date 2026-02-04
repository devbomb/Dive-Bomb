using System;
using System.Runtime.CompilerServices;
using Godot;

namespace FastDragon
{
    public partial class SignalBus : Node
    {
        public static SignalBus Instance {get; private set;}

        // Why are we using user signals instead of the [Signal] attribute?
        // It's a workaround for this bug:
        // https://github.com/godotengine/godot/issues/82346
        //
        // tldr: If you use the += syntax to connect to a signal created using
        // the [Signal] attribute, the Callable won't be disconnected when the
        // handler node is QueueFreed()'d (EG: because you changed levels).
        // That's a problem for us because SignalBus is a singleton that
        // persists between level changes.
        public event Action LevelReset
        {
            add => ConnectAction(value);
            remove => DisconnectAction(value);
        }

        /// <summary>
        ///     Emitted when the player activates a checkpoint.
        ///
        ///     You can use this to allow certain story flags to only "stick"
        ///     if the player reaches a checkpoint before their next death.
        /// </summary>
        public event Action CheckpointActivated
        {
            add => ConnectAction(value);
            remove => DisconnectAction(value);
        }

        public event Action ExitReached
        {
            add => ConnectAction(value);
            remove => DisconnectAction(value);
        }

        public event Action<string> CycleStarted
        {
            add => ConnectAction(value);
            remove => DisconnectAction(value);
        }

        public SignalBus()
        {
            AddUserSignal(nameof(LevelReset));
            AddUserSignal(nameof(CheckpointActivated));
            AddUserSignal(nameof(ExitReached));

            AddUserSignal(nameof(CycleStarted),
            [
                new Godot.Collections.Dictionary
                {
                    {"name", "cycleId"},
                    {"type", (int)Variant.Type.String}
                }
            ]);
        }

        public override void _Ready()
        {
            Instance = this;
        }

        public void EmitLevelReset() => EmitSignal(nameof(LevelReset));
        public void EmitCheckpointActivated() => EmitSignal(nameof(CheckpointActivated));
        public void EmitExitReached() => EmitSignal(nameof(ExitReached));
        public void EmitCycleStarted(string cycleId) => EmitSignal(nameof(CycleStarted), cycleId);

        private void ConnectAction(
            Action action,
            [CallerMemberName] string signalName = ""
        )
        {
            Connect(signalName, Callable.From(action));
        }

        private void ConnectAction<T>(
            Action<T> action,
            [CallerMemberName] string signalName = ""
        )
        {
            Connect(signalName, Callable.From(action));
        }

        private void DisconnectAction(
            Action action,
            [CallerMemberName] string signalName = ""
        )
        {
            Disconnect(signalName, Callable.From(action));
        }

        private void DisconnectAction<T>(
            Action<T> action,
            [CallerMemberName] string signalName = ""
        )
        {
            Disconnect(signalName, Callable.From(action));
        }
    }
}