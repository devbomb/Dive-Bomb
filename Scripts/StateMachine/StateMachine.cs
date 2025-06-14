using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class StateMachine : Node
    {
        public delegate void StateChangingEventHandler(State currentState, State incomingState);
        public event StateChangingEventHandler StateChanging;

        public State CurrentState { get; private set; }
        private readonly Type _stateType;

        private readonly List<State> _stateCache = new List<State>();

        public StateMachine(Type stateType)
        {
            _stateType = stateType;
        }

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

        public void ChangeState<TState>() where TState : State, new()
        {
            ChangeState(typeof(TState));
        }

        public void ChangeState(Type stateType)
        {
            if (!stateType.IsAssignableTo(_stateType))
                throw new Exception($"{stateType.Name} is not a {_stateType.Name}");

            // Get the incoming state's node.
            // If it doesn't exist yet, create it.
            State incomingState = _stateCache.FirstOrDefault(s => s.GetType() == stateType);
            if (incomingState == null)
            {
                incomingState = (State)Activator.CreateInstance(stateType);
                incomingState.SetStateMachine(this);
                _stateCache.Add(incomingState);
            }

            StateChanging?.Invoke(CurrentState, incomingState);

            // Let the previous state know that it's exiting
            CurrentState?.OnStateExited();

            // Switch to the new state and enable it.
            State prevState = CurrentState;
            CurrentState = incomingState;
            CurrentState.OnStateEntered(prevState);
            CurrentState.OnStateEntered();
        }
    }
}