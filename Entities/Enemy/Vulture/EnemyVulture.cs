using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class EnemyVulture : CharacterBody3D, IDamageable, IGemContainer
    {
        public const float MaxSpeed = Player.Walk.Speed * 1.5f;
        public const float Accel = 32;
        public const float RotSpeedDeg = 360;

        [Signal] public delegate void KilledEventHandler();

        [Export] public GemColor GemColor { get; set; } = GemColor.Red;
        [Export] public float AggroRange = 20;

        [ExportCategory("Internal")]
        [Export] public CollisionShape3D BodyShape;
        [Export] public Node3D Model;
        [Export] public AggroSphere AggroSphere;
        [Export] public AnimationPlayer AnimationPlayer;
        [Export] public FuseBombProjectile Bomb;


        public bool IsDead => _stateMachine.CurrentState is Dead;
        public bool Invulnerable => IsDead;

        private StateMachine _stateMachine = new StateMachine();

        private Vector3 _spawnPoint;
        private Vector3 _spawnRotation;
        private Player _targetPlayer;

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

            _stateMachine.ChangeState<Idle>();
        }

        public void OnDamaged()
        {
            _stateMachine.ChangeState<Dead>();

            Bomb.GlobalPosition = GlobalPosition;
            Bomb.ResetPhysicsInterpolation3D();
            Bomb.Reveal();
        }

        private void RefreshAggroSphereSize()
        {
            AggroSphere.Radius = AggroRange;
        }

        private bool IsTouchingPlayer()
        {
            int collisionCount = GetSlideCollisionCount();
            for (int i = 0; i < collisionCount; i++)
            {
                var collision = GetSlideCollision(i);
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

        private class Idle : State<EnemyVulture>
        {
            public override void OnStateEntered()
            {
                Self.GlobalPosition = Self._spawnPoint;
                Self.ResetPhysicsInterpolation3D();

                Self.AnimationPlayer.Play("Idle");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                Self.GlobalRotation = Self.GlobalRotation.RotateTowardEulerRad(
                    Self._spawnRotation,
                    Mathf.DegToRad(RotSpeedDeg) * delta
                );

                var player = Self.AggroSphere.SearchForPlayer();
                if (player != null)
                    ChangeState<Alerted>();
            }
        }

        private class Alerted : State<EnemyVulture>
        {
            public const double Duration = 0.75;
            public const float RiseHeight = 1;

            private double _timer;
            private Vector3 _startPos;
            private Vector3 _endPos;

            public override void OnStateEntered()
            {
                Self._targetPlayer = Self.AggroSphere.SearchForPlayer();

                _startPos = Self.GlobalPosition;
                _endPos = _startPos + (Vector3.Up * RiseHeight);
                _timer = 0;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                float t = (float)(_timer / (Duration * 0.9));
                t = 1f - Mathf.Pow(t - 1, 4);
                Self.GlobalPosition = _startPos.Lerp(_endPos, t);

                if (_timer >= Duration)
                    ChangeState<Chasing>();
            }
        }

        private class Chasing : State<EnemyVulture>
        {
            private const float PreferredDistance = 3;
            private const float CatchUpSpeed = Player.Walk.Speed * 1.5f;
            private const float CruiseSpeed = Player.Walk.Speed * 1.1f;

            public override void OnStateEntered()
            {
                Self.AnimationPlayer.Play("Fly");
            }

            public override void _PhysicsProcess(double delta)
            {
                var targetPoint = Self._targetPlayer.GlobalPosition;

                // Don't chase the player upwards unless line of sight has been
                // broken.  That way, the player can jump over the vulture
                // while still letting it fly over obstacles
                if (Self.AggroSphere.HasLineOfSightTo(Self._targetPlayer))
                {
                    if (targetPoint.Y > Self.GlobalPosition.Y)
                        targetPoint.Y = Self.GlobalPosition.Y;
                }

                // Speed up if we're too far away from the player, slow down
                // if we're too close.
                float distance = Self.GlobalPosition.DistanceTo(targetPoint);
                float fspeed = distance > PreferredDistance
                    ? CatchUpSpeed
                    : CruiseSpeed;

                Self.Velocity = fspeed * Self.GlobalPosition.DirectionTo(targetPoint);
                Self.MoveAndSlide();

                Self.GlobalRotation = Self.GlobalRotation.RotateTowardEulerRad(
                    Self.Velocity.Normalized().ForwardToEulerAnglesRad(),
                    Mathf.DegToRad(RotSpeedDeg) * (float)delta
                );

                if (Self.IsTouchingPlayer())
                    OnTouchedPlayer();
            }

            private void OnTouchedPlayer()
            {
                Self._targetPlayer.TryDamage<PlayerDamageFlipState>(1);
                ChangeState<Returning>();
            }
        }

        private class Returning : State<EnemyVulture>
        {
            public override void OnStateEntered()
            {
                Self.AnimationPlayer.Play("Fly");
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

        private class Dead : State<EnemyVulture>
        {
            public override void OnStateEntered()
            {
                Self.BodyShape.Disabled = true;
                Self.Model.Visible = false;
                Self.EmitSignal(EnemyVulture.SignalName.Killed);
            }

            public override void OnStateExited()
            {
                Self.BodyShape.Disabled = false;
                Self.Model.Visible = true;
            }
        }
    }
}
