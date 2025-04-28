using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class StateMachine : Node
    {
        [Signal] public delegate void StateChangingEventHandler(State currentState, State incomingState);

        public State CurrentState {get; private set;}
        private readonly Type _stateType;

        public StateMachine(Type stateType)
        {
            _stateType = stateType;
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
            State incomingState = States().FirstOrDefault(s => s.GetType() == stateType);
            if (incomingState == null)
            {
                incomingState = (State)Activator.CreateInstance(stateType);
                AddChild(incomingState);
            }

            EmitSignal(SignalName.StateChanging, CurrentState, incomingState);

            // Let the previous state know that it's exiting
            CurrentState?.OnStateExited();

            // Disable the previous state.
            // ...and all the other states, too, just for good measure.
            foreach (var state in States())
            {
                state.ProcessMode = ProcessModeEnum.Disabled;
            }

            // Switch to the new state and enable it.
            State prevState = CurrentState;
            CurrentState = incomingState;
            CurrentState.OnStateEntered(prevState);
            CurrentState.OnStateEntered();

            // Defer enabling the new state to ensure consistency.
            // This way, we ensure the new state's first Process(or PhysicsProcess)
            // always happens on the _next_ frame, instead of sometimes happening
            // on the _current_ frame depending on tree order.
            //
            // Doing this as a function call (instead of SetDeferred) avoids a
            // bug where multiple states could become enabled simulatneously if
            // ChangeState() is called more than once in the same frame
            // (since multiple SetDeferreds would be queued up simultaneously).
            Callable.From(EnableCurrentState).CallDeferred();
        }

        private void EnableCurrentState()
        {
            CurrentState.ProcessMode = ProcessModeEnum.Inherit;
        }

        private IEnumerable<State> States()
        {
            for (int i = 0; i < GetChildCount(); i++)
            {
                var child = GetChild<Node>(i);

                if (child is State state)
                    yield return state;
            }
        }
    }
}