using Godot;

namespace FastDragon
{
    public partial class EnemyHandCannoner : StaticBody3D, IChargeable
    {
        [Export] public GemColor GemColor = GemColor.Red;

        public bool IsAlive =>
            !(_stateMachine.CurrentState is Dieing) &&
            !(_stateMachine.CurrentState is Dead);

        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%Animator");
        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(EnemyHandCannonerState));
        private Gem _gem;

        public override void _Ready()
        {
            AddChild(_stateMachine);

            _gem = GemFactory.Create(GemColor);
            _gem.StartHidden = true;
            AddChild(_gem);

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            // TODO: Stay dead if all of the following are true:
            // * The enemy is dead (or dieing)
            // * The player has collected the enemy's gem
            // * The player has reached a checkpoint since killing the enemy
            _stateMachine.ChangeState<Sleeping>();
        }

        public void OnCharged()
        {
            if (IsAlive)
                _stateMachine.ChangeState<Dieing>();
        }

        private partial class EnemyHandCannonerState : State
        {
            protected EnemyHandCannoner _enemy => _stateMachine.GetParent<EnemyHandCannoner>();
        }

        private partial class Sleeping : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                _enemy._animator.Play("Sleep");
            }
        }

        private partial class WakingUp : EnemyHandCannonerState {}
        private partial class Shielding : EnemyHandCannonerState {}
        private partial class Aiming : EnemyHandCannonerState {}
        private partial class RecoilingAfterFiring : EnemyHandCannonerState {}

        private partial class Dieing : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                _enemy._gem.Reveal();
                _enemy._animator.Play("Death");
            }

            public override void _PhysicsProcess(double delta)
            {
                if (!_enemy._animator.IsPlaying())
                    ChangeState<Dead>();
            }
        }

        private partial class Dead : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                _enemy.Visible = false;
                _enemy._bodyShape.Disabled = true;
            }

            public override void OnStateExited()
            {
                _enemy.Visible = true;
                _enemy._bodyShape.Disabled = false;
            }
        }
    }
}