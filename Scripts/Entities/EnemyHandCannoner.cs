using Godot;

namespace FastDragon
{
    public partial class EnemyHandCannoner : StaticBody3D, IRollable
    {
        [Export] public GemColor GemColor = GemColor.Red;
        [Export] public PackedScene ProjectilePrefab;
        [Export] public float ShieldDuration = 1.5f;
        [Export] public float AimDuration = 1;
        [Export] public float RecoilDuration = 1;

        public bool IsAlive =>
            !(_stateMachine.CurrentState is Dieing) &&
            !(_stateMachine.CurrentState is Dead);

        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%Animator");
        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");
        private AggroSphere _aggroSphere => GetNode<AggroSphere>("%AggroSphere");
        private Node3D _projectileSpawn => GetNode<Node3D>("%ProjectileSpawnPoint");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(EnemyHandCannonerState));
        private Gem _gem;

        private Player _targetPlayer = null;

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
            _targetPlayer = null;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            var state = (EnemyHandCannonerState)_stateMachine.CurrentState;
            _bodyShape.Disabled = !state.EnableCollision;
        }

        public void OnRolledInto()
        {
            if (IsAlive)
                _stateMachine.ChangeState<Dieing>();
        }

        private void FireProjectile()
        {
            _stateMachine.ChangeState<RecoilingAfterFiring>();
            var projectile = ProjectilePrefab.Instantiate<PhysicsBody3D>();

            GetTree().CurrentScene.AddChild(projectile);
            projectile.GlobalPosition = _projectileSpawn.GlobalPosition;
            projectile.GlobalRotation = _projectileSpawn.GlobalRotation;
            projectile.AddCollisionExceptionWith(this);
        }

        private void FaceTargetPlayer()
        {
            if (_targetPlayer == null)
                return;

            GlobalRotation = GlobalPosition
                    .DirectionTo(_targetPlayer.GlobalPosition)
                    .Flattened()
                    .Normalized()
                    .ForwardToEulerAnglesRad();
        }

        private partial class EnemyHandCannonerState : State
        {
            public virtual bool EnableCollision => true;

            protected EnemyHandCannoner _enemy => _stateMachine.GetParent<EnemyHandCannoner>();
        }

        private partial class Sleeping : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                _enemy._animator.Play("Sleep");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _enemy._targetPlayer = _enemy._aggroSphere.SearchForPlayer();
                if (_enemy._targetPlayer != null)
                    ChangeState<WakingUp>();
            }
        }

        private partial class WakingUp : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                _enemy._animator.Play("WakeUp", 0.1f);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _enemy.FaceTargetPlayer();

                if (!_enemy._animator.IsPlaying())
                    ChangeState<Shielding>();
            }
        }

        private partial class Shielding : EnemyHandCannonerState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = _enemy.ShieldDuration;
                _enemy._animator.Play("Shield", 0.2f);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _enemy.FaceTargetPlayer();
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<Aiming>();
            }
        }

        private partial class Aiming : EnemyHandCannonerState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = _enemy.AimDuration;
                _enemy._animator.Play("Aim", 0.2f);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _enemy.FaceTargetPlayer();
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    _enemy.FireProjectile();
            }
        }

        private partial class RecoilingAfterFiring : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                _enemy._animator.Play("FireRecoil");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (!_enemy._animator.IsPlaying())
                    ChangeState<Shielding>();
            }
        }

        private partial class Dieing : EnemyHandCannonerState
        {
            public override bool EnableCollision => false;

            public override void OnStateEntered()
            {
                _enemy.FaceTargetPlayer();
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
            public override bool EnableCollision => false;

            public override void OnStateEntered()
            {
                _enemy.Visible = false;
            }

            public override void OnStateExited()
            {
                _enemy.Visible = true;
            }
        }
    }
}