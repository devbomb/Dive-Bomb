using Godot;

namespace FastDragon
{
    public partial class LevelExitCanon : Node3D
    {
        [Export] public bool StartHidden = false;
        [Export] public float HiddenHeight = -6;
        [Export] public double RevealDuration = 1;

        private BreakableStaticBody3D _crystal => GetNode<BreakableStaticBody3D>("%Crystal");
        private CollisionShape3D _crystalShape => GetNode<CollisionShape3D>("%CrystalCollisionShape");
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
            if (StartHidden)
                _stateMachine.ChangeState<Hidden>();
            else
                _stateMachine.ChangeState<Revealed>();
        }

        public void Reveal()
        {
            _stateMachine.ChangeState<Revealing>();
        }

        private void SetCollisionEnabled(bool enabled)
        {
            _crystalShape.Disabled = !enabled;
        }

        private class Hidden : State<LevelExitCanon>
        {
            public override void OnStateEntered()
            {
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
            private Vector3 _hiddenPosition;

            public override void OnStateEntered()
            {
                _hiddenPosition = Self._revealedPosition + (Vector3.Up * Self.HiddenHeight);
                Self.GlobalPosition = _hiddenPosition;
                Self.ResetPhysicsInterpolation3D();
                _timer = 0;
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer += delta;

                float t = (float)(_timer / Self.RevealDuration);
                t = Mathf.SmoothStep(0, 1, t);
                Self.GlobalPosition = _hiddenPosition.Lerp(Self._revealedPosition, t);

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
    }
}