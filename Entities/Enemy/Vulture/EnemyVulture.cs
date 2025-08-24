using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class EnemyVulture : CharacterBody3D, IBreakable, IGemContainer
    {
        public const float MaxSpeed = Player.Walk.Speed * 1.5f;
        public const float Accel = 32;
        public const float RotSpeedDeg = 360;

        [Signal] public delegate void KilledEventHandler();

        [Export] public GemColor GemColor { get; set; } = GemColor.Red;
        [Export] public float AggroRange = 20;

        public bool IsDead => _stateMachine.CurrentState is Dead;

        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");
        private Node3D _model => GetNode<Node3D>("%Model");
        private AggroSphere _aggroSphere => GetNode<AggroSphere>("%AggroSphere");

        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%AnimationPlayer");
        private StateMachine _stateMachine = new StateMachine();

        private Vector3 _spawnPoint;
        private Vector3 _spawnRotation;

        public override void _Ready()
        {
            base._Ready();

            AddChild(_stateMachine);

            RefreshAggroSphereSize();

            SignalBus.Instance.LevelReset += Reset;
            _spawnPoint = GlobalPosition;
            _spawnRotation = GlobalRotation;

            Reset();
        }

        public void Reset()
        {
            GlobalPosition = _spawnPoint;
            GlobalRotation = _spawnRotation;
            Velocity = Vector3.Zero;
            this.ResetPhysicsInterpolation3D();

            // TODO: Stay dead if all of the following are true:
            // * The enemy is dead (or dieing)
            // * The player has collected the enemy's gem
            // * The player has reached a checkpoint since killing the enemy
            _stateMachine.ChangeState<Idle>();
        }

        public void OnBroken()
        {
            if (!IsDead) _stateMachine.ChangeState<Dead>();
        }

        private void RefreshAggroSphereSize()
        {
            _aggroSphere.Radius = AggroRange;
        }

        private class VultureState : State<EnemyVulture> {}

        private class Idle : VultureState
        {
            public override void OnStateEntered()
            {
                Self.GlobalPosition = Self._spawnPoint;
                Self.ResetPhysicsInterpolation3D();

                Self._animator.Play("Idle");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                Self.GlobalRotation = Self.GlobalRotation.RotateTowardEulerRad(
                    Self._spawnRotation,
                    Mathf.DegToRad(RotSpeedDeg) * delta
                );

                var player = Self._aggroSphere.SearchForPlayer();
                if (player != null)
                    ChangeState<Chasing>();
            }
        }

        private class Chasing : VultureState
        {
            private Player _targetPlayer;
            private float _fspeed;

            public override void OnStateEntered()
            {
                _targetPlayer = Self._aggroSphere.SearchForPlayer();
                _fspeed = 0;
                Self.Velocity = Vector3.Zero;
                Self._animator.Play("Fly");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _fspeed = Mathf.MoveToward(_fspeed, MaxSpeed, delta * Accel);
                Self.Velocity = _fspeed * Self.GlobalPosition.DirectionTo(_targetPlayer.GlobalPosition);
                Self.MoveAndSlide();

                Self.GlobalRotation = Self.GlobalRotation.RotateTowardEulerRad(
                    Self.Velocity.Normalized().ForwardToEulerAnglesRad(),
                    Mathf.DegToRad(RotSpeedDeg) * delta
                );

                if (IsTouchingPlayer())
                    OnTouchedPlayer();
            }

            private bool IsTouchingPlayer()
            {
                int collisionCount = Self.GetSlideCollisionCount();
                for (int i = 0; i < collisionCount; i++)
                {
                    var collision = Self.GetSlideCollision(i);
                    int colliderCount = collision.GetCollisionCount();
                    for (int j = 0; j < colliderCount; j++)
                    {
                        var collider = collision.GetCollider(j);
                        if (collider is Player)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            private void OnTouchedPlayer()
            {
                _targetPlayer.TryDamage<PlayerDamageFlipState>();
                ChangeState<Returning>();
            }
        }

        private class Returning : VultureState
        {
            public override void OnStateEntered()
            {
                Self._animator.Play("Fly");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                Self.GlobalPosition = Self.GlobalPosition.MoveToward(
                    Self._spawnPoint,
                    MaxSpeed * delta
                );

                Vector3 targetRot = Self.GlobalPosition
                    .DirectionTo(Self._spawnPoint)
                    .ForwardToEulerAnglesRad();

                Self.GlobalRotation = Self.GlobalRotation.RotateTowardEulerRad(
                    targetRot,
                    Mathf.DegToRad(RotSpeedDeg) * delta
                );

                if (Self.Position.IsEqualApprox(Self._spawnPoint))
                {
                    ChangeState<Idle>();
                }
            }
        }

        private class Dead : VultureState
        {
            public override void OnStateEntered()
            {
                Self._bodyShape.Disabled = true;
                Self._model.Visible = false;
                Self.EmitSignal(EnemyVulture.SignalName.Killed);
            }

            public override void OnStateExited()
            {
                Self._bodyShape.Disabled = false;
                Self._model.Visible = true;
            }
        }
    }
}
