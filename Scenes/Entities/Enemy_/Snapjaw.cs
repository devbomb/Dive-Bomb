using Godot;
using System;

namespace FastDragon
{
    public partial class Snapjaw : Node3D
    {
        private AnimationTree _animator => GetNode<AnimationTree>("%AnimationTree");
        private readonly StateMachine _stateMachine = new StateMachine(typeof(SnapjawState));

        public override void _Ready()
        {
            AddChild(_stateMachine);
            SignalBus.Instance.LevelReset += Reset;

            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<Looping>();
        }

        public void OnBroken()
        {
            _stateMachine.ChangeState<Dead>();
        }

        private abstract partial class SnapjawState : State
        {
            protected Snapjaw _self => _stateMachine.GetParent<Snapjaw>();
        }

        private partial class Looping : SnapjawState
        {
            public override void OnStateEntered()
            {
                _self._animator.PlayState("Attack");
            }
        }

        private partial class Dead : SnapjawState
        {
            public override void OnStateEntered()
            {
                _self._animator.PlayState("RESET");
                _self.Visible = false;
            }

            public override void OnStateExited()
            {
                _self.Visible = true;
            }
        }
    }
}
