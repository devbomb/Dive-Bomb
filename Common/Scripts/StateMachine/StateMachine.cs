using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class StateMachine : Node
    {
        public delegate void StateChangingEventHandler(IState currentState, IState incomingState);
        public event StateChangingEventHandler StateChanging;

        public IState CurrentState { get; private set; }

        private readonly List<IState> _stateCache = new List<IState>();

        public override void _Input(InputEvent ev)
        {
            CurrentState?._Input(ev);
        }

        public override void _Process(double delta)
        {
            CurrentState?._Process(delta);
        }

        public override void _PhysicsProcess(double delta)
        {
            CurrentState?._PhysicsProcess(delta);
        }

        public override void _ExitTree()
        {
            // Give the current state a chance to un-subscribe from any
            // SignalBus signals, to avoid a memory leak (and potential
            // ObjectDisposedExceptions).
            //
            // It will have a chance to re-subscribe to them again if the
            // state machine is added back to the tree.
            CurrentState?.UnsubscribeFromSignals();
        }

        public override void _EnterTree()
        {
            CurrentState?.SubscribeToSignals();
        }

        public void ChangeState<TState>() where TState : IState, new()
        {
            var stateType = typeof(TState);

            // Get the incoming state's node.
            // If it doesn't exist yet, create it.
            IState incomingState = _stateCache.FirstOrDefault(s => s.GetType() == stateType);
            if (incomingState == null)
            {
                incomingState = new TState();
                incomingState.SetStateMachine(this);
                _stateCache.Add(incomingState);
            }

            StateChanging?.Invoke(CurrentState, incomingState);

            // Let the previous state know that it's exiting
            CurrentState?.OnStateExited();
            CurrentState?.UnsubscribeFromSignals();

            // Switch to the new state and enable it.
            IState prevState = CurrentState;
            CurrentState = incomingState;
            CurrentState.SubscribeToSignals();
            CurrentState.OnStateEntered(prevState);
            CurrentState.OnStateEntered();
        }
    }
}