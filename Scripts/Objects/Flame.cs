using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    [Tool]
    public partial class Flame : Node3D
    {
        public const float ActiveDuration = 0.5f;
        public const float CooldownDuration = 7f / 30;

        [Export] public PackedScene FlameTendrilPrefab;
        [Export] public Node3D BodyToIgnore;

        [Export] public float Length
        {
            get => _length;
            set => SetProperty(ref _length, value);
        }
        private float _length = 3f;

        [Export] public float ConeAngleDeg
        {
            get => _coneAngleDeg;
            set => SetProperty(ref _coneAngleDeg, value);
        }
        private float _coneAngleDeg = 50;

        [Export] public float ConeYScale
        {
            get => _coneYScale;
            set => SetProperty(ref _coneYScale, value);
        }
        private float _coneYScale = 0.75f;

        [Export] public int ShellTendrilCount
        {
            get => _shellTendrilCount;
            set => SetProperty(ref _shellTendrilCount, value);
        }
        private int _shellTendrilCount = 5;

        [Export] public int InnerRingTendrilCount
        {
            get => _innerRingTendrilCount;
            set => SetProperty(ref _innerRingTendrilCount, value);
        }
        private int _innerRingTendrilCount = 4;

        private Node3D _tendrils => GetNode<Node3D>("%Tendrils");

        private bool _initialized = false;

        private StateMachine _stateMachine;
        private bool _ready => _stateMachine.CurrentState is ReadyState;

        private float _timer;

        public override void _Ready()
        {
            _initialized = true;
            CreateFlameTendrils();

            if (!Engine.IsEditorHint())
            {
                SignalBus.Instance.LevelReset += Reset;

                _stateMachine = new StateMachine(typeof(FlameState));
                AddChild(_stateMachine);

                Reset();
            }
        }

        public void Reset()
        {
            _stateMachine.ChangeState<ReadyState>();
        }

        public override void _Input(InputEvent ev)
        {
            bool allowFlaming = this.FirstAncestor<Player>().AllowFlaming;

            if (InputService.FlameJustPressed(ev) && _ready && allowFlaming)
                _stateMachine.ChangeState<Flaming>();
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is IFlamable flamable)
                flamable.OnFlamed();
        }

        private void CreateFlameTendrils()
        {
            // Clear out existing tendrils
            foreach (var tendril in AllFlameTendrils().ToArray())
            {
                tendril.GetParent().RemoveChild(tendril);
                tendril.QueueFree();
            }

            CreateTendril();
            CreateRing(ShellTendrilCount, ConeAngleDeg);
            CreateRing(InnerRingTendrilCount, ConeAngleDeg / 2);

            void CreateRing(int tendrilCount, float hAngleDeg)
            {
                for (int i = 0; i < tendrilCount; i++)
                {
                    float t = ((float)i) / (tendrilCount - 1);
                    float thetaDeg = Mathf.Lerp(0, 180, t);

                    var tendril = CreateTendril();
                    Vector3 forward = Vector3.Forward
                        .Rotated(Vector3.Up, Mathf.DegToRad(hAngleDeg / 2))
                        .Rotated(Vector3.Forward, Mathf.DegToRad(thetaDeg));

                    forward.Y *= ConeYScale;
                    forward = forward.Normalized();

                    tendril.Rotation = forward.ForwardToEulerAnglesRad();
                }
            }

            FlameTendril CreateTendril()
            {
                var tendril = FlameTendrilPrefab.Instantiate<FlameTendril>();
                tendril.BodyToIgnore = BodyToIgnore;
                tendril.MaxLength = Length;
                tendril.ActiveDuration = ActiveDuration;

                _tendrils.AddChild(tendril);
                return tendril;
            }
        }

        private IEnumerable<FlameTendril> AllFlameTendrils()
        {
            return _tendrils.EnumerateChildren().Cast<FlameTendril>();
        }

        private void SetProperty<T>(ref T storage, T value)
        {
            storage = value;

            if (Engine.IsEditorHint() && _initialized && Owner != this)
                CreateFlameTendrils();
        }

        private partial class ReadyState : FlameState {}

        private partial class Flaming : FlameState
        {
            public override void OnStateEntered()
            {
                _flame._timer = ActiveDuration;

                foreach (var tendril in _flame.AllFlameTendrils())
                    tendril.Start();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _flame._timer -= delta;

                if (_flame._timer <= 0)
                    ChangeState<CoolingDown>();
            }
        }

        private partial class CoolingDown : FlameState
        {
            public override void OnStateEntered()
            {
                _flame._timer = CooldownDuration;

                foreach (var tendril in _flame.AllFlameTendrils())
                    tendril.Stop();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _flame._timer -= delta;

                if (_flame._timer <= 0)
                    ChangeState<ReadyState>();
            }
        }

        private abstract partial class FlameState : State
        {
            protected Flame _flame => _stateMachine.GetParent<Flame>();
        }
    }
}