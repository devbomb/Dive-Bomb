using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public abstract partial class State : Node
    {
        public bool IsCurrent => _stateMachine.CurrentState == this;

        protected StateMachine _stateMachine => GetParent<StateMachine>();

        public virtual void OnStateEntered(State prevState) {}
        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}

        protected void ChangeState<TState>() where TState : State, new()
        {
            _stateMachine.ChangeState<TState>();
        }
    }
}