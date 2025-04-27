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
        // handler node is QueueFreed()'d (EG: because you changed maps).
        // That's a problem for us because SignalBus is a singleton that
        // persists between map changes.
        public event Action LevelReset
        {
            add => ConnectAction(value);
            remove => DisconnectAction(value);
        }

        public event Action ExitReached
        {
            add => ConnectAction(value);
            remove => DisconnectAction(value);
        }

        public SignalBus()
        {
            AddUserSignal(nameof(LevelReset));
            AddUserSignal(nameof(ExitReached));
        }

        public override void _Ready()
        {
            Instance = this;
        }

        public void EmitLevelReset() => EmitSignal(nameof(LevelReset));
        public void EmitExitReached() => EmitSignal(nameof(ExitReached));

        private void ConnectAction(
            Action action,
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
    }
}