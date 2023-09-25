using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class Flame : Node3D
    {
        public const float ActiveDuration = 0.5f;
        public const float CooldownDuration = 7f / 30;
        public const float Length = 1.5f;
        public const float HAngleDeg = 65f;
        public const float VAngleDeg = 15f;

        [Export] public PackedScene FlameTendrilPrefab;
        [Export] public Node3D BodyToIgnore;

        public bool AllowFlaming = true;

        private Node3D _tendrils => GetNode<Node3D>("%Tendrils");

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
            SignalBus.Instance.LevelReset += Reset;

            // Create a bunch of tendrils
            for (int i = 0; i < 4; i++)
            {
                float t = ((float)i) / 4;
                float hAngleDeg = Mathf.Lerp(-HAngleDeg / 2, HAngleDeg / 2, t);
                AddTendril(hAngleDeg, 0);
                AddTendril(hAngleDeg, VAngleDeg);
            }

            void AddTendril(float hAngleDeg, float vAngleDeg)
            {
                var tendril = FlameTendrilPrefab.Instantiate<FlameTendril>();
                tendril.RotationDegrees = new Vector3(vAngleDeg, hAngleDeg, 0);
                tendril.BodyToIgnore = BodyToIgnore;

                _tendrils.AddChild(tendril);
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
            float delta = (float)deltaD;

            switch (_currentState)
            {
                case State.Flaming:
                {
                    _timer -= delta;

                    foreach (var tendril in AllFlameTendrils())
                    {
                        tendril.Length = Mathf.Lerp(Length, 0, _timer / ActiveDuration);
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

        private IEnumerable<FlameTendril> AllFlameTendrils()
        {
            return _tendrils.EnumerateChildren().Cast<FlameTendril>();
        }
    }
}