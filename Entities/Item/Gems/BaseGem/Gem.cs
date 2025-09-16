using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class Gem : CharacterBody3D
    {
        public const float HomingDuration = 0.5f;
        public const float FlameChargeWindowDuration = 0.1f;
        public const float RevealJumpVelocity = 10;
        public const float Gravity = 30;

        public bool IsCollected => this.GetLevel()
            ?.GetProgress()
            ?.IsGemCollected(Value, SaveKey) ?? false;

        [Export] public GemColor Value;

        public bool StartHidden = false;
        public bool TouchedGroundOnce {get; private set;} = false;
        public bool IsRevealed => _stateMachine.CurrentState is Revealed;

        public string SaveKey { get; private set; }

        public Area3D CollectionArea => GetNode<Area3D>("%CollectionArea");

        private AnimationPlayer _spinAnim => GetNode<AnimationPlayer>("%SpinAnimator");
        private AnimationPlayer _sparkleAnim => GetNode<AnimationPlayer>("%SparkleAnimator");

        private AudioStreamPlayer _homeInSound => GetNode<AudioStreamPlayer>("%HomeInSound");
        private AudioStreamPlayer _collectSound => GetNode<AudioStreamPlayer>("%CollectSound");

        // Need to actually store this node instead of using a getter, since
        // we temporarily remove it from the scene tree.  If the getter were
        // called during a temporary removal, it would result in a
        // NullReferenceException.
        private VisibleOnScreenEnabler3D _visibleEnabler;
        private Node _visibleEnablerParent;

        private Transform3D _initialPos;
        private StateMachine _stateMachine = new StateMachine();

        public override void _Ready()
        {
            SaveKey = GenerateSaveKey();
            base._Ready();

            _visibleEnabler = GetNode<VisibleOnScreenEnabler3D>("%VisibleEnabler");
            _visibleEnablerParent = _visibleEnabler.GetParent();

            _initialPos = GlobalTransform;
            AddChild(_stateMachine);
            Reset();

            SignalBus.Instance.LevelReset += Reset;

            _spinAnim.Seek(GD.Randf() * _spinAnim.CurrentAnimationLength);
        }

        public void Reset()
        {
            GlobalTransform = _initialPos;
            Velocity = Vector3.Zero;
            this.ResetPhysicsInterpolation3D();

            if (StartHidden || IsCollected)
                ChangeState<Hidden>();
            else
                ChangeState<Revealed>();
        }

        public void OnCollectionAreaBodyEntered(Node3D body)
        {
            if (body is Player && IsRevealed)
                StartHomingIn();
        }

        /// <summary>
        /// Reveals the gem, if it isn't already collected.
        /// </summary>
        public void Reveal()
        {
            if (!IsCollected)
                ChangeState<Revealed>();
        }

        public void StartHomingIn()
        {
            ChangeState<Homing>();
        }

        public void Collect()
        {
            var level = this.GetLevel();
            if (level != null)
            {
                level.GetProgress().CollectGem(Value, SaveKey);

                if (!level.TimeTrial.IsTimeTrialMode)
                    SaveFileManager.Current.AddUntalliedGem(Value);
            }

            _collectSound.Play();
            ChangeState<Hidden>();

            SaveFileManager.Instance.RequestAutosave();
        }

        public void Sparkle()
        {
            _sparkleAnim.Play("Sparkle");
        }

        private string GenerateSaveKey()
        {
            var builder = new System.Text.StringBuilder();
            Visit(this);
            return builder.ToString();

            void Visit(Node n)
            {
                if (n.GetParent() == GetTree().Root)
                {
                    builder.Append(n.Name);
                    return;
                }

                Visit(n.GetParent());
                builder.Append("/");
                builder.Append(n.GetIndex());
            }
        }

        private void ChangeState<TState>() where TState : State<Gem>, new()
        {
            _stateMachine.ChangeState<TState>();
        }

        private Node3D GetPlayer()
        {
            var player = GetTree().Root
                .EnumerateDescendants()
                .First(n => n is Player);

            return (Node3D)player;
        }

        private void EnableVisibilityEnabler()
        {
            if (!_visibleEnabler.IsInsideTree())
                _visibleEnablerParent.AddChild(_visibleEnabler);
        }

        private void DisableVisibilityEnabler()
        {
            if (_visibleEnabler.IsInsideTree())
            {
                _visibleEnablerParent.RemoveChild(_visibleEnabler);
                ProcessMode = ProcessModeEnum.Inherit;
            }
        }

        private class Hidden : State<Gem>
        {
            public override void OnStateEntered()
            {
                Self.Visible = false;
            }

            public override void OnStateExited()
            {
                Self.Visible = true;
            }
        }
        private class Revealed : State<Gem>
        {
            private float _flameChargeWindowTimer = 0;
            private Area3D _flameChargeArea => Self.GetNode<Area3D>("%FlameChargeArea");

            public override void OnStateEntered()
            {
                SetCollision(true);
                Self.TouchedGroundOnce = false;

                if (Self.StartHidden)
                {
                    Self.Velocity = Vector3.Up * RevealJumpVelocity;
                    _flameChargeWindowTimer = FlameChargeWindowDuration;
                }
            }

            public override void OnStateExited()
            {
                SetCollision(false);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                if (!Self.TouchedGroundOnce)
                {
                    Self.Velocity += Vector3.Down * Gravity * delta;

                    var collision = Self.MoveAndCollide(Self.Velocity * delta);
                    if (collision != null)
                    {
                        Self.Velocity = Vector3.Zero;

                        if (collision.GetCollider() is StaticBody3D)
                        {
                            Self.TouchedGroundOnce = true;
                            SetCollision(false);
                        }
                    }
                }

                if (_flameChargeWindowTimer > 0)
                {
                    _flameChargeWindowTimer -= delta;
                    HomeInIfPlayerChargingNearby();
                }
            }

            private void HomeInIfPlayerChargingNearby()
            {
                bool shouldHomeIn = _flameChargeArea
                    .GetOverlappingBodies()
                    .Any(n => n is Player);

                if (shouldHomeIn)
                    Self.StartHomingIn();
            }

            private void SetCollision(bool enabled)
            {
                Self.GetNode<CollisionShape3D>("%PhysicsShape")
                    .SetDeferred("disabled", !enabled);
            }
        }
        private class Homing : State<Gem>
        {
            private Vector3 _homingStartPos;
            private float _homingTimer;

            public override void OnStateEntered()
            {
                _homingStartPos = Self.GlobalPosition;
                _homingTimer = 0;
                Self._homeInSound.Play();

                // HACK: Don't let the gem fall asleep when going off-screen,
                // so the player doesn't get screwed over for moving too fast.
                // Do this by temporarily removing the visibility detector while
                // in this state.
                Self.DisableVisibilityEnabler();
            }

            public override void OnStateExited()
            {
                Self.EnableVisibilityEnabler();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _homingTimer += delta;
                float t = _homingTimer / HomingDuration;
                t = FastSlowFast(t, 1.4f);

                Vector3 start = _homingStartPos;
                Vector3 end = Self.GetPlayer().GlobalPosition + (Vector3.Up * 0.25f);
                Vector3 control = GetTree().Root.GetCamera3D().GlobalPosition + (Vector3.Up * 3);

                Self.GlobalPosition = start.LerpBezier(end, control, t);

                Vector3 forward = (Self.GlobalPosition - control).Normalized();
                Vector3 targetRot = forward.ForwardToEulerAnglesRad();

                float decayRate = 5f;
                Self.GlobalRotation = new Vector3(
                    AngleMath.DecayToward(Self.GlobalRotation.X, targetRot.X, decayRate, delta),
                    AngleMath.DecayToward(Self.GlobalRotation.Y, targetRot.Y, decayRate, delta),
                    AngleMath.DecayToward(Self.GlobalRotation.Z, targetRot.Z, decayRate, delta)
                );

                if (_homingTimer >= HomingDuration)
                {
                    Self.Collect();
                }
            }

            private float FastSlowFast(float t, float exponent)
            {
                t = (2 * t) - 1;
                float c = Mathf.Sign(t) * Mathf.Pow(Mathf.Abs(t), exponent);
                return (c + 1) / 2;
            }
        }
    }
}