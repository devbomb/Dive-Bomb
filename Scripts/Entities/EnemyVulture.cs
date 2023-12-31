using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class EnemyVulture : InterpolatedCharacterBody3D, IChargeable, IFlamable
    {
        public const float MaxSpeed = Player.Walk.Speed * 1.5f;
        public const float Accel = 32;
        public const float RotSpeedDeg = 360;

        [Export] public GemColor GemColor = GemColor.Red;
        [Export] public float AggroRange = 20;

        public bool IsDead => _stateMachine.CurrentState is Dead;

        private CollisionShape3D _bodyShape => GetNode<CollisionShape3D>("%BodyShape");
        private Area3D _aggroRangeArea => GetNode<Area3D>("%AggroRangeArea");
        private Node3D _model => GetNode<Node3D>("%Model");
        private Gem _gem;

        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%AnimationPlayer");
        private RayCast3D _losDetector => GetNode<RayCast3D>("%LosDetector");

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
            ResetPhysicsInterpolation();

            // TODO: Stay dead if all of the following are true:
            // * The enemy is dead (or dieing)
            // * The player has collected the enemy's gem
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

        private void RefreshAggroSphereSize()
        {
            var sphereShape = _aggroRangeArea
                .EnumerateDescendantsOfType<CollisionShape3D>()
                .First()
                .Shape as SphereShape3D;

            sphereShape.Radius = AggroRange;
        }

        private Player FirstPlayerInRange()
        {
            return _aggroRangeArea
                    .GetOverlappingBodies()
                    .Where(body => body is Player)
                    .Cast<Player>()
                    .FirstOrDefault();
        }

        private bool HasLineOfSightTo(Player player)
        {
            var localPlayerPos = _losDetector.ToLocal(player.GlobalPosition);
            _losDetector.TargetPosition = localPlayerPos;
            _losDetector.ForceUpdateTransform();
            _losDetector.ForceRaycastUpdate();

            var collider = _losDetector.GetCollider();
            return (collider == null || collider == player);
        }

        private partial class VultureState : State
        {
            protected EnemyVulture _vulture => _stateMachine.GetParent<EnemyVulture>();
        }

        private partial class Idle : VultureState
        {
            private int _framesToWait;

            public override void OnStateEntered()
            {
                _vulture.GlobalPosition = _vulture._spawnPoint;
                _vulture.ResetPhysicsInterpolation();
                _framesToWait = 2;

                _vulture._animator.Play("Idle");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                // HACK: Wait 2 frames before calling FirstPlayerInRange(), to
                // avoid a false positive.
                //
                // If the level resets while the player is still within aggro
                // range, then there is a 2 physics-frame window where, for some
                // reason, FirstPlayerInRange() thinks the player is still in
                // range.  If we don't wait out that 2-frame window, then the
                // vulture will immediately re-aggro after the player respawns,
                // regardless of how far away they are.
                //
                // TODO: Figure out why this happens.  Until then, blame Godot.
                if (_framesToWait > 0)
                {
                    _framesToWait--;
                    return;
                }

                _vulture.GlobalRotation = _vulture.GlobalRotation.RotateTowardEulerRad(
                    _vulture._spawnRotation,
                    Mathf.DegToRad(RotSpeedDeg) * delta
                );

                var player = _vulture.FirstPlayerInRange();
                if (player != null && _vulture.HasLineOfSightTo(player))
                    ChangeState<Chasing>();
            }
        }

        private partial class Chasing : VultureState
        {
            private Player _targetPlayer;
            private float _fspeed;

            public override void OnStateEntered()
            {
                _targetPlayer = _vulture.FirstPlayerInRange();
                _fspeed = 0;
                _vulture.Velocity = Vector3.Zero;
                _vulture._animator.Play("Fly");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _fspeed = Mathf.MoveToward(_fspeed, MaxSpeed, delta * Accel);
                _vulture.Velocity = _fspeed * _vulture.GlobalPosition.DirectionTo(_targetPlayer.GlobalPosition);
                _vulture.MoveAndSlide();

                _vulture.GlobalRotation = _vulture.GlobalRotation.RotateTowardEulerRad(
                    _vulture.Velocity.Normalized().ForwardToEulerAnglesRad(),
                    Mathf.DegToRad(RotSpeedDeg) * delta
                );

                if (IsTouchingPlayer())
                    OnTouchedPlayer();
            }

            private bool IsTouchingPlayer()
            {
                int collisionCount = _vulture.GetSlideCollisionCount();
                for (int i = 0; i < collisionCount; i++)
                {
                    var collision = _vulture.GetSlideCollision(i);
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

        private partial class Returning : VultureState
        {
            public override void OnStateEntered()
            {
                _vulture._animator.Play("Fly");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _vulture.GlobalPosition = _vulture.GlobalPosition.MoveToward(
                    _vulture._spawnPoint,
                    MaxSpeed * delta
                );

                Vector3 targetRot = _vulture.GlobalPosition
                    .DirectionTo(_vulture._spawnPoint)
                    .ForwardToEulerAnglesRad();

                _vulture.GlobalRotation = _vulture.GlobalRotation.RotateTowardEulerRad(
                    targetRot,
                    Mathf.DegToRad(RotSpeedDeg) * delta
                );

                if (_vulture.Position.IsEqualApprox(_vulture._spawnPoint))
                {
                    ChangeState<Idle>();
                }
            }
        }

        private partial class Dead : VultureState
        {
            public override void OnStateEntered()
            {
                _vulture._bodyShape.Disabled = true;
                _vulture._model.Visible = false;
                _vulture._gem.Reveal();
            }

            public override void OnStateExited()
            {
                _vulture._bodyShape.Disabled = false;
                _vulture._model.Visible = true;
            }
        }
    }
}
