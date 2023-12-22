using Godot;

namespace FastDragon
{
    public partial class EnemyVulture : InterpolatedCharacterBody3D, IChargeable, IFlamable
    {
        [Export] public GemColor GemColor = GemColor.Red;

        public bool IsDead => _stateMachine.CurrentState is Dead;

        private CollisionShape3D _collisionShape => GetNode<CollisionShape3D>("%CollisionShape");
        private Node3D _model => GetNode<Node3D>("%Model");
        private Gem _gem;

        private StateMachine _stateMachine = new StateMachine(typeof(VultureState));

        private Vector3 _spawnPoint;
        private Vector3 _spawnRotation;

        public override void _Ready()
        {
            base._Ready();

            AddChild(_stateMachine);

            _gem = GemFactory.Create(GemColor);
            _gem.StartHidden = true;
            _gem.Name = "Gem";
            AddChild(_gem);

            SignalBus.Instance.LevelReset += Reset;
            _spawnPoint = Position;
            _spawnRotation = Rotation;

            Reset();
        }

        public void Reset()
        {
            Position = _spawnPoint;
            Rotation = _spawnRotation;
            Velocity = Vector3.Zero;
            ResetPhysicsInterpolation();

            // TODO: Stay dead if all of the following are true:
            // * The enemy is dead
            // * The player has not collected the enemy's gem
            // * The player has reached a checkpoint since killing the enemy
            _stateMachine.ChangeState<Idle>();
        }

        public void OnCharged()
        {
            if (!IsDead) _stateMachine.ChangeState<Dead>();
        }

        public void OnFlamed()
        {
            if (!IsDead) _stateMachine.ChangeState<Dead>();
        }

        private partial class VultureState : State
        {
            protected EnemyVulture _vulture => _stateMachine.GetParent<EnemyVulture>();
        }

        private partial class Idle : VultureState {}

        private partial class Dead : VultureState
        {
            public override void OnStateEntered()
            {
                _vulture._collisionShape.Disabled = true;
                _vulture._model.Visible = false;

                bool gemCollected = SaveFile.Current.IsGemCollected(_vulture._gem.GetSaveKey());
                if (!gemCollected)
                {
                    _vulture._gem.Reveal();
                }
            }

            public override void OnStateExited()
            {
                _vulture._collisionShape.Disabled = false;
                _vulture._model.Visible = true;
            }
        }
    }
}
