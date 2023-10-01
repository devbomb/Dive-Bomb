using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class StateMachine : Node
    {
        public State CurrentState {get; private set;}
        private readonly Type _stateType;

        public StateMachine(Type stateType)
        {
            _stateType = stateType;
        }

        public void ChangeState<TState>() where TState : State, new()
        {
            if (!typeof(TState).IsAssignableTo(_stateType))
                throw new Exception($"{typeof(TState).Name} is not a {_stateType.Name}");

            CurrentState?.OnStateExited();

            foreach (var state in States())
            {
                state.ProcessMode = ProcessModeEnum.Disabled;
            }

            CurrentState = States().FirstOrDefault(s => s is TState);
            if (CurrentState == null)
            {
                CurrentState = new TState();
                AddChild(CurrentState);
            }

            CurrentState.ProcessMode = ProcessModeEnum.Inherit;
            CurrentState.OnStateEntered();
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