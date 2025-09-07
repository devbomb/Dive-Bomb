using Godot;

namespace FastDragon
{
    public partial class LevelExitCanon : Node3D
    {
        [Export] public bool StartHidden = false;
        [Export] public float HiddenHeight = -6;
        [Export] public double RevealDuration = 1;

        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%Animator");
        private BreakableStaticBody3D _crystal => GetNode<BreakableStaticBody3D>("%Crystal");
        private CollisionShape3D _crystalShape => GetNode<CollisionShape3D>("%CrystalCollisionShape");
        private Node3D _playerLandPoint => GetNode<Node3D>("%PlayerLandPoint");

        private readonly StateMachine _stateMachine = new StateMachine();

        private Vector3 _revealedPosition;

        public override void _Ready()
        {
            AddChild(_stateMachine);

            _revealedPosition = GlobalPosition;

            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            _animator.Play("RESET");
            _animator.Advance(0);

            if (StartHidden)
                _stateMachine.ChangeState<Hidden>();
            else
                _stateMachine.ChangeState<Revealed>();
        }

        public void Reveal()
        {
            _stateMachine.ChangeState<Revealing>();
        }

        public void OnCrystalShattered()
        {
            if (this.GetLevel() != null)
            {
                this.GetLevel().GetProgress().ExitReached = true;
            }

            SignalBus.Instance.EmitExitReached();

            _stateMachine.ChangeState<AligningPlayer>();
        }

        private void SetCollisionEnabled(bool enabled)
        {
            _crystalShape.Disabled = !enabled;
        }

        private bool IsTimeTrialMode() => this.GetLevel()?.TimeTrial.IsTimeTrialMode ?? false;

        private Vector3 HiddenPos() => _revealedPosition + (Vector3.Up * HiddenHeight);

        private class Hidden : State<LevelExitCanon>
        {
            public override void OnStateEntered()
            {
                Self.GlobalPosition = Self.HiddenPos();
                Self.ResetPhysicsInterpolation3D();

                Self.Visible = false;
                Self.SetCollisionEnabled(false);
            }

            public override void OnStateExited()
            {
                Self.Visible = true;
                Self.SetCollisionEnabled(true);
            }
        }

        private class Revealing : State<LevelExitCanon>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                Self.GlobalPosition = Self.HiddenPos();
                Self.ResetPhysicsInterpolation3D();
                _timer = 0;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                float t = (float)(_timer / Self.RevealDuration);
                t = Mathf.SmoothStep(0, 1, t);
                Self.GlobalPosition = Self.HiddenPos().Lerp(Self._revealedPosition, t);

                if (_timer > Self.RevealDuration)
                    ChangeState<Revealed>();
            }

            public override void OnStateExited()
            {
                Self.GlobalPosition = Self._revealedPosition;
            }
        }

        private class Revealed : State<LevelExitCanon>
        {
            public override void OnStateEntered()
            {
                Self._crystal.Rollable = true;
            }

            public override void OnStateExited()
            {
                Self._crystal.Rollable = false;
            }
        }

        private class AligningPlayer : State<LevelExitCanon>
        {
            private const double Duration = 0.5;
            private Player _player;
            private Vector3 _playerStartPos;
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = 0;

                _player = Self.GetTree().FindNode<Player>();
                _player.ChangeState<PlayerManhandledState>();

                _playerStartPos = _player.GlobalPosition;

                // Make the player face toward the canon's center
                var lookPos = Self.GlobalPosition;
                lookPos.Y = _player.GlobalPosition.Y;
                _player.LookAt(lookPos);
                _player.ResetPhysicsInterpolation3D();
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                float t = (float)(_timer / Duration);
                t = Mathf.Clamp(t, 0, 1);

                var pos = _playerStartPos.Lerp(Self._playerLandPoint.GlobalPosition, t);
                pos.Y = Mathf.Lerp(_playerStartPos.Y, Self._playerLandPoint.GlobalPosition.Y, t * t);
                _player.GlobalPosition = pos;

                // TODO: Move to the next state
                if (_timer > Duration)
                    ChangeState<PlayerLanded>();
            }
        }

        private class PlayerLanded : State<LevelExitCanon>
        {
            private const double Duration = 1.5;
            private double _timer;

            public override void OnStateEntered()
            {
                var player = Self.GetTree().FindNode<Player>();
                player.GlobalPosition = Self._playerLandPoint.GlobalPosition;
                player.ResetPhysicsInterpolation3D();

                player.Animator.Play("ParachuteLand");
                _timer = Duration;

                Self._animator.Play("Phoomp");
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;
                if (_timer <= 0)
                    ChangeState<BlastingOff>();
            }
        }

        private class BlastingOff : State<LevelExitCanon>
        {
            private const float InitRotSpeedDeg = 360;
            private const float FinalRotSpeedDeg = 180;
            private const float RotSpeedAccelDeg = 180;

            private Player _player;
            private float _rotSpeedDeg;

            private bool _detectedAnimationFinished;

            public override void OnStateEntered()
            {
                Self._animator.Play("MissionClear");

                _player = GetTree().FindNode<Player>();
                _player.Animator.Play("Glide");
                _player.Camera.Shake(2, 10, 0.5f);

                _rotSpeedDeg = InitRotSpeedDeg;
                _detectedAnimationFinished = false;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                // Spin the player and move them up
                float speed = 40;
                _player.GlobalRotationDegrees += Vector3.Up * _rotSpeedDeg * delta;

                _rotSpeedDeg = Mathf.MoveToward(
                    _rotSpeedDeg,
                    FinalRotSpeedDeg,
                    RotSpeedAccelDeg * delta
                );

                _player.GlobalPosition += Vector3.Up * speed * delta;

                // Rotate the camera underneath the player
                _player.Camera.OrbitPitchRad = AngleMath.DecayToward(
                    _player.Camera.OrbitPitchRad,
                    Mathf.DegToRad(89.999f),
                    2,
                    delta
                );

                // Move on when the animation is finished
                if (!Self._animator.IsPlaying() && !_detectedAnimationFinished)
                {
                    _detectedAnimationFinished = true;

                    if (Self.IsTimeTrialMode())
                    {
                        Self.GetLevel().TimeTrial.ShowResultsScreen();
                        return;
                    }

                    LevelTransitionManager.Instance.ExitLevel();
                }
            }
        }
    }
}