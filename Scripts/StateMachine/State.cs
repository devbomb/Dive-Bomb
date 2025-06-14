using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace FastDragon
{
    public abstract class State
    {
        public bool IsCurrent => _stateMachine.CurrentState == this;

        protected StateMachine _stateMachine;

        public void SetStateMachine(StateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        protected SceneTree GetTree() => _stateMachine.GetTree();
        protected Viewport GetViewport() => _stateMachine.GetViewport();

        public virtual void OnStateEntered(State prevState) {}
        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}

        public virtual void _Input(InputEvent ev) {}

        public virtual void _Process(double delta) {}
        public virtual void _PhysicsProcess(double delta) {}

        protected void ChangeState<TState>() where TState : State, new()
        {
            _stateMachine.ChangeState<TState>();
        }
    }
}