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
        void OnStateExited(IState nextState) {}

        void OnStateEntered() {}
        void OnStateExited() {}

        void _Input(InputEvent ev) {}

        void _Process(double delta) {}
        void _PhysicsProcess(double delta) {}

        /// <summary>
        ///     Override this to subscribe to SignalBus signals.  Make sure to
        ///     unsubscribe from them again in <see cref="UnsubscribeFromSignals"/>
        ///     to avoid a memory leak (and potential ObjectDisposedExceptions).
        ///
        ///     Called when the state is entered (BEFORE <see cref="OnStateEntered"/>),
        ///     OR when the state machine is added to the tree while in this
        ///     state.
        /// </summary>
        void SubscribeToSignals();

        /// <summary>
        ///     Override this to unsubscribe from signals you subscribed to in
        ///     <see cref="SubscribeToSignals"/>.
        ///
        ///     Called when the state is exited (AFTER <see cref="OnStateExited"/>),
        ///     OR when the state machine is removed from the tree while in this
        ///     state.
        /// </summary>
        void UnsubscribeFromSignals();
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
        public virtual void OnStateExited(IState nextState) {}

        public virtual void OnStateEntered() {}
        public virtual void OnStateExited() {}

        public virtual void _Input(InputEvent ev) {}

        public virtual void _Process(double delta) {}
        public virtual void _PhysicsProcess(double delta) {}

        protected void ChangeState<TState>() where TState : IState, new()
        {
            _stateMachine.ChangeState<TState>();
        }

        /// <inheritdoc />
        public virtual void SubscribeToSignals() {}
        /// <inheritdoc />
        public virtual void UnsubscribeFromSignals() {}
    }
}