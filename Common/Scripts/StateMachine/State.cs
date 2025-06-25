using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Serilog.Debugging;

namespace FastDragon
{
    public interface IState
    {
        void SetStateMachine(StateMachine stateMachine);

        void OnStateEntered(IState prevState) {}
        void OnStateEntered() {}
        void OnStateExited() {}

        void _Input(InputEvent ev) {}

        void _Process(double delta) {}
        void _PhysicsProcess(double delta) {}
    }

    public abstract class State<TSelf> : IState where TSelf : Node
    {
        public bool IsCurrent => _stateMachine.CurrentState == this;

        protected TSelf Self => _stateMachine.GetParent<TSelf>();

        private StateMachine _stateMachine;

        public void SetStateMachine(StateMachine stateMachine)
        {
            _stateMachine = stateMachine;
        }

        protected SceneTree GetTree() => _stateMachine.GetTree();
        protected Viewport GetViewport() => _stateMachine.GetViewport();

        public virtual void OnStateEntered(IState prevState) {}
        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}

        public virtual void _Input(InputEvent ev) {}

        public virtual void _Process(double delta) {}
        public virtual void _PhysicsProcess(double delta) {}

        protected void ChangeState<TState>() where TState : IState, new()
        {
            _stateMachine.ChangeState<TState>();
        }
    }
}