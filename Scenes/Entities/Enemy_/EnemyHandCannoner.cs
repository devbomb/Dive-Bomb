using Godot;

namespace FastDragon
{
    public partial class EnemyHandCannoner : StaticBody3D, IBreakable, IGemContainer
    {
        public bool VulnerableToKick => false;

        [Signal] public delegate void KilledEventHandler();

        [Export] public GemColor GemColor { get; set; } = GemColor.Red;
        [Export] public PackedScene ProjectilePrefab;
        [Export] public float ShieldDuration = 1.5f;
        [Export] public float AimDuration = 1;
        [Export] public float RecoilDuration = 1;
        [Export] public float AggroRadius = 20;

        public bool IsAlive =>
            !(_stateMachine.CurrentState is Dieing) &&
            !(_stateMachine.CurrentState is Dead);

        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%Animator");
        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");
        private AggroSphere _aggroSphere => GetNode<AggroSphere>("%AggroSphere");
        private Node3D _projectileSpawn => GetNode<Node3D>("%ProjectileSpawnPoint");

        private Node3D _model => GetNode<Node3D>("%Model");
        private Node3D _blobShadow => GetNode<Node3D>("%BlobShadow");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(EnemyHandCannonerState));

        private Player _targetPlayer = null;

        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Reset;
            Reset();

            _aggroSphere.Radius = AggroRadius;
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

        public void OnBroken()
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

        private partial class EnemyHandCannonerState : State<EnemyHandCannoner>
        {
            public virtual bool EnableCollision => true;
        }

        private partial class Sleeping : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                Self._animator.Play("Sleep");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                Self._targetPlayer = Self._aggroSphere.SearchForPlayer();
                if (Self._targetPlayer != null)
                    ChangeState<WakingUp>();
            }
        }

        private partial class WakingUp : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                Self._animator.Play("WakeUp", 0.1f);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                Self.FaceTargetPlayer();

                if (!Self._animator.IsPlaying())
                    ChangeState<Shielding>();
            }
        }

        private partial class Shielding : EnemyHandCannonerState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = Self.ShieldDuration;
                Self._animator.Play("Shield", 0.2f);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                Self.FaceTargetPlayer();
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
                _timer = Self.AimDuration;
                Self._animator.Play("Aim", 0.2f);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                Self.FaceTargetPlayer();
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    Self.FireProjectile();
            }
        }

        private partial class RecoilingAfterFiring : EnemyHandCannonerState
        {
            public override void OnStateEntered()
            {
                Self._animator.Play("FireRecoil");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (!Self._animator.IsPlaying())
                    ChangeState<Shielding>();
            }
        }

        private partial class Dieing : EnemyHandCannonerState
        {
            public override bool EnableCollision => false;

            public override void OnStateEntered()
            {
                Self.FaceTargetPlayer();
                Self.EmitSignal(EnemyHandCannoner.SignalName.Killed);
                Self._animator.Play("Death");
            }

            public override void _PhysicsProcess(double delta)
            {
                if (!Self._animator.IsPlaying())
                    ChangeState<Dead>();
            }
        }

        private partial class Dead : EnemyHandCannonerState
        {
            public override bool EnableCollision => false;

            public override void OnStateEntered()
            {
                Self._model.Visible = false;
                Self._blobShadow.Visible = false;
            }

            public override void OnStateExited()
            {
                Self._model.Visible = true;
                Self._blobShadow.Visible = true;
            }
        }
    }
}