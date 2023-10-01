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

        [Export] public GemColor Value;

        public bool StartHidden = false;
        public bool TouchedGroundOnce {get; private set;} = false;
        public bool IsRevealed => _stateMachine.CurrentState is Revealed;

        private AnimationPlayer _spinAnim => GetNode<AnimationPlayer>("%SpinAnimator");
        private AnimationPlayer _sparkleAnim => GetNode<AnimationPlayer>("%SparkleAnimator");

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

            bool alreadyCollected = SaveFile.Current
                .CollectedGems
                .Contains(GetPath());

            if (StartHidden || alreadyCollected)
                ChangeState<Hidden>();
            else
                ChangeState<Revealed>();
        }
        public void OnCollectionAreaBodyEntered(Node3D body)
        {
            if (body is Player && IsRevealed)
                StartHomingIn();
        }

        public void Reveal()
        {
            ChangeState<Revealed>();
        }

        public void StartHomingIn()
        {
            ChangeState<Homing>();
        }

        public void Collect()
        {
            SaveFile.Current.TotalGemCount += (int)Value;
            SaveFile.Current.CollectedGems.Add(GetPath());
            ChangeState<Hidden>();

            GD.Print($"{SaveFile.Current.TotalGemCount}: Collected gem {GetPath()}");
        }

        public void Sparkle()
        {
            _sparkleAnim.Play("Sparkle");
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
            private Node3D _blobShadow => _gem.GetNode<Node3D>("%BlobShadow");
            private Area3D _flameChargeArea => _gem.GetNode<Area3D>("%FlameChargeArea");

            public override void OnStateEntered()
            {
                _gem.TouchedGroundOnce = false;
                _blobShadow.Scale = Vector3.One;

                if (_gem.StartHidden)
                {
                    _gem.Velocity = Vector3.Up * RevealJumpVelocity;
                    _flameChargeWindowTimer = FlameChargeWindowDuration;
                }
            }

            public override void OnStateExited()
            {
                _blobShadow.Scale = Vector3.Zero;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _gem.Velocity += Vector3.Down * Gravity * delta;

                var collision = _gem.MoveAndCollide(_gem.Velocity * delta);
                if (collision != null)
                {
                    _gem.Velocity = Vector3.Zero;
                    _gem.TouchedGroundOnce = true;
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
                    .Any(n => n is Player p && p.SpawningGemsHomeIn);

                if (shouldHomeIn)
                    _gem.StartHomingIn();
            }
        }
        private partial class Homing : GemState
        {
            private Vector3 _homingStartPos;
            private float _homingTimer;

            public override void OnStateEntered()
            {
                _homingStartPos = _gem.GlobalPosition;
                _homingTimer = 0;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _homingTimer += delta;
                float t = _homingTimer / HomingDuration;

                Vector3 start = _homingStartPos;
                Vector3 end = _gem.GetPlayer().GlobalPosition + (Vector3.Up * 0.25f);
                Vector3 control = GetTree().Root.GetCamera3D().GlobalPosition + (Vector3.Up * 3);

                _gem.GlobalPosition = BezierCurve(start, end, control, t);

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

            private Vector3 BezierCurve(
                Vector3 start,
                Vector3 end,
                Vector3 control,
                float t
            )
            {
                var a = start.Lerp(control, t);
                var b = start.Lerp(end, t);
                return a.Lerp(b, t);
            }
        }

        private abstract partial class GemState : State
        {
            protected Gem _gem => _stateMachine.GetParent<Gem>();
        }
    }
}