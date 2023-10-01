using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class StateMachine : Node
    {
        private State _currentState;
        private readonly Type _stateType;

        public StateMachine(Type stateType)
        {
            _stateType = stateType;
        }

        public void ChangeState<TState>() where TState : State, new()
        {
            if (!typeof(TState).IsAssignableTo(_stateType))
                throw new Exception($"{typeof(TState).Name} is not a {_stateType.Name}");

            _currentState?.OnStateExited();

            foreach (var state in States())
            {
                state.ProcessMode = ProcessModeEnum.Disabled;
            }

            _currentState = States().FirstOrDefault(s => s is TState);
            if (_currentState == null)
            {
                _currentState = new TState();
                AddChild(_currentState);
            }

            _currentState.ProcessMode = ProcessModeEnum.Inherit;
            _currentState.OnStateEntered();
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