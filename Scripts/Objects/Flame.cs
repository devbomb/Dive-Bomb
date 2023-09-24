using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class Flame : Node3D
    {
        public const float ActiveDuration = 0.5f;
        public const float CooldownDuration = 7f / 30;
        public const float Length = 1.5f;

        public bool AllowFlaming = true;

        private Node3D _model => GetNode<Node3D>("%Model");
        private CollisionShape3D _hitBoxShape => GetNode<CollisionShape3D>("%HitBoxShape");

        private enum State
        {
            Ready,
            Flaming,
            CoolingDown
        }
        private State _currentState = State.Ready;
        private bool _ready => _currentState == State.Ready;

        private float _timer;

        public override void _Input(InputEvent ev)
        {
            if (InputService.FlameJustPressed(ev) && _ready && AllowFlaming)
                StartFlaming();
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
        }

        public void Reset()
        {
            BecomeReady();
            AllowFlaming = true;
        }

        public override void _PhysicsProcess(double deltaD)
        {
            float delta = (float)deltaD;

            _model.Visible = _currentState == State.Flaming;
            _hitBoxShape.Disabled = _currentState != State.Flaming;

            switch (_currentState)
            {
                case State.Flaming:
                {
                    _timer -= delta;

                    foreach (var tendril in AllFlameTendrils())
                    {
                        tendril.Length = Mathf.Lerp(Length, 0, _timer / ActiveDuration);
                        tendril.ParticleSpeed = Length / ActiveDuration;
                    }

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
        }

        private void StartCoolingDown()
        {
            _currentState = State.CoolingDown;
            _timer = CooldownDuration;
        }

        private void BecomeReady()
        {
            _currentState = State.Ready;
        }

        private IEnumerable<FlameTendril> AllFlameTendrils()
        {
            foreach (var desc in this.EnumerateDescendants())
            {
                if (desc is FlameTendril tendril)
                {
                    yield return tendril;
                }
            }
        }
    }
}