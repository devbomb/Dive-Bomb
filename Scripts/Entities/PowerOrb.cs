using Godot;

namespace FastDragon
{
    // TODO: Don't use inheritance, ya twit!
    public partial class PowerOrb : BreakableStaticBody3D
    {
        public bool IsBroken => _stateMachine.CurrentState is BrokenState;

        private Node3D _model => GetNode<Node3D>("%Model");
        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(PowerOrbState));

        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Reset;
            Broken += Break;

            Reset();
        }

        private void Reset()
        {
            SetHidden();
        }

        public void Reveal()
        {
            _stateMachine.ChangeState<Revealed>();
        }

        public void SetHidden()
        {
            _stateMachine.ChangeState<Hidden>();
        }

        public void Break()
        {
            _stateMachine.ChangeState<BrokenState>();
        }

        private abstract partial class PowerOrbState : State
        {
            protected PowerOrb _self => _stateMachine.GetParent<PowerOrb>();
        }

        private partial class Revealed : PowerOrbState
        {
        }

        private partial class Hidden : PowerOrbState
        {
            public override void OnStateEntered()
            {
                _self._bodyShape.Disabled = true;
                _self._model.Visible = false;
            }

            public override void OnStateExited()
            {
                _self._bodyShape.Disabled = false;
                _self._model.Visible = true;
            }
        }

        private partial class BrokenState : PowerOrbState
        {
            public override void OnStateEntered()
            {
                // TODO: Play particle effects
                _self._bodyShape.Disabled = true;
                _self._model.Visible = false;
            }

            public override void OnStateExited()
            {
                _self._bodyShape.Disabled = false;
                _self._model.Visible = true;
            }
        }
    }
}