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

        public bool AllowFlaming = true;

        private Node3D _tendrils => GetNode<Node3D>("%Tendrils");

        private bool _initialized = false;

        private enum State
        {
            Ready,
            Flaming,
            CoolingDown
        }
        private State _currentState = State.Ready;
        private bool _ready => _currentState == State.Ready;

        private float _timer;

        public override void _Ready()
        {
            _initialized = true;
            CreateFlameTendrils();

            if (!Engine.IsEditorHint())
            {
                SignalBus.Instance.LevelReset += Reset;
            }
        }

        public override void _Input(InputEvent ev)
        {
            if (InputService.FlameJustPressed(ev) && _ready && AllowFlaming)
                StartFlaming();
        }

        public void Reset()
        {
            BecomeReady();
            AllowFlaming = true;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            if (Engine.IsEditorHint())
                return;

            float delta = (float)deltaD;

            switch (_currentState)
            {
                case State.Flaming:
                {
                    _timer -= delta;

                    if (_timer <= 0)
                        StartCoolingDown();

                    break;
                }

                case State.CoolingDown:
                {
                    _timer -= delta;

                    if (_timer <= 0)
                        BecomeReady();

                    break;
                }
            }
        }

        public void OnBodyEntered(Node3D body)
        {
            if (body is IFlamable flamable)
                flamable.OnFlamed();
        }

        private void StartFlaming()
        {
            _currentState = State.Flaming;
            _timer = ActiveDuration;

            foreach (var tendril in AllFlameTendrils())
                tendril.Start();
        }

        private void StartCoolingDown()
        {
            _currentState = State.CoolingDown;
            _timer = CooldownDuration;

            foreach (var tendril in AllFlameTendrils())
                tendril.Stop();
        }

        private void BecomeReady()
        {
            _currentState = State.Ready;
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
    }
}