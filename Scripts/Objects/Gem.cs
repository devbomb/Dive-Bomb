using Godot;
using System.Linq;

namespace FastDragon
{
    public partial class Gem : InterpolatedCharacterBody3D
    {
        public const float HomingDuration = 0.5f;
        public const float FlameChargeWindowDuration = 0.1f;
        public const float RevealJumpVelocity = 10;
        public const float Gravity = 30;

        public bool IsCollected => SaveFile.Current
            .CollectedGems
            .Contains(GetSaveKey());

        [Export] public GemColor Value;

        public bool StartHidden = false;
        public bool TouchedGroundOnce {get; private set;} = false;
        public bool IsRevealed => _stateMachine.CurrentState is Revealed;

        public Area3D CollectionArea => GetNode<Area3D>("%CollectionArea");

        private AnimationPlayer _spinAnim => GetNode<AnimationPlayer>("%SpinAnimator");
        private AnimationPlayer _sparkleAnim => GetNode<AnimationPlayer>("%SparkleAnimator");
        private VisibleOnScreenEnabler3D _visibleEnabler => GetNode<VisibleOnScreenEnabler3D>("%VisibleEnabler");

        private Vector3 _initialPos;
        private StateMachine _stateMachine = new StateMachine(typeof(GemState));

        public override void _Ready()
        {
            base._Ready();

            _initialPos = Position;
            AddChild(_stateMachine);
            Reset();

            SignalBus.Instance.LevelReset += Reset;

            _spinAnim.Seek(GD.Randf() * _spinAnim.CurrentAnimationLength);
        }

        public void Reset()
        {
            Position = _initialPos;
            Velocity = Vector3.Zero;
            ResetPhysicsInterpolation();

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
            var saveFile = SaveFile.Current;
            if (!saveFile.UntalliedGems.ContainsKey(Value))
            {
                saveFile.UntalliedGems[Value] = 0;
            }
            saveFile.UntalliedGems[Value]++;
            saveFile.GetMapProgress(saveFile.CurrentMap).GemsCollected += (int)Value;
            saveFile.CollectedGems.Add(GetSaveKey());

            ChangeState<Hidden>();

            GD.Print($"{saveFile.TotalGemCount}: Collected gem {GetSaveKey()}");
        }

        public void Sparkle()
        {
            _sparkleAnim.Play("Sparkle");
        }

        public string GetSaveKey()
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

        private void ChangeState<TState>() where TState : GemState, new()
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

        private partial class Hidden : GemState
        {
            public override void OnStateEntered()
            {
                _gem.Visible = false;
            }

            public override void OnStateExited()
            {
                _gem.Visible = true;
            }
        }
        private partial class Revealed : GemState
        {
            private float _flameChargeWindowTimer = 0;
            private Area3D _flameChargeArea => _gem.GetNode<Area3D>("%FlameChargeArea");

            public override void OnStateEntered()
            {
                SetCollision(true);
                _gem.TouchedGroundOnce = false;

                if (_gem.StartHidden)
                {
                    _gem.Velocity = Vector3.Up * RevealJumpVelocity;
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

                if (!_gem.TouchedGroundOnce)
                {
                    _gem.Velocity += Vector3.Down * Gravity * delta;

                    var collision = _gem.MoveAndCollide(_gem.Velocity * delta);
                    if (collision != null)
                    {
                        _gem.Velocity = Vector3.Zero;

                        if (collision.GetCollider() is StaticBody3D)
                        {
                            _gem.TouchedGroundOnce = true;
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
                    _gem.StartHomingIn();
            }

            private void SetCollision(bool enabled)
            {
                _gem.GetNode<CollisionShape3D>("%PhysicsShape")
                    .SetDeferred("disabled", !enabled);
            }
        }
        private partial class Homing : GemState
        {
            private Vector3 _homingStartPos;
            private float _homingTimer;

            private VisibleOnScreenEnabler3D _storedVisibleEnabler;
            private Node _visibleEnablerParent;

            public override void OnStateEntered()
            {
                _homingStartPos = _gem.GlobalPosition;
                _homingTimer = 0;

                // HACK: Don't let the gem fall asleep when going off-screen,
                // so the player doesn't get screwed over for moving too fast.
                // Do this by temporarily removing the visibility detector while
                // in this state.
                _storedVisibleEnabler = _gem._visibleEnabler;
                _visibleEnablerParent = _storedVisibleEnabler.GetParent();
                _visibleEnablerParent.RemoveChild(_storedVisibleEnabler);
                _gem.ProcessMode = ProcessModeEnum.Inherit;
            }

            public override void OnStateExited()
            {
                _visibleEnablerParent.AddChild(_storedVisibleEnabler);
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _homingTimer += delta;
                float t = _homingTimer / HomingDuration;

                Vector3 start = _homingStartPos;
                Vector3 end = _gem.GetPlayer().GlobalPosition + (Vector3.Up * 0.25f);
                Vector3 control = GetTree().Root.GetCamera3D().GlobalPosition + (Vector3.Up * 3);

                _gem.GlobalPosition = start.LerpBezier(end, control, t);

                Vector3 forward = (_gem.GlobalPosition - control).Normalized();
                Vector3 targetRot = forward.ForwardToEulerAnglesRad();

                float decayRate = 5f;
                _gem.GlobalRotation = new Vector3(
                    AngleMath.DecayToward(_gem.GlobalRotation.X, targetRot.X, decayRate, delta),
                    AngleMath.DecayToward(_gem.GlobalRotation.Y, targetRot.Y, decayRate, delta),
                    AngleMath.DecayToward(_gem.GlobalRotation.Z, targetRot.Z, decayRate, delta)
                );

                if (_homingTimer >= HomingDuration)
                {
                    _gem.Collect();
                }
            }
        }

        private abstract partial class GemState : State
        {
            protected Gem _gem => _stateMachine.GetParent<Gem>();
        }
    }
}