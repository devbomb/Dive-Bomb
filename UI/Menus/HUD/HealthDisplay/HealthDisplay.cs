using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class HealthDisplay : Control
    {
        [Export] public TextureProgressBar ProgressBar;
        [Export] public Control RestPoint;
        [Export] public Control CenterPoint;

        private Player _player;

        private int _lastHealth;

        private readonly StateMachine _stateMachine = new();

        public HealthDisplay()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Callable.From(() =>
            {
                _player = GetTree().Root.FindNode<Player>();
                Reset();
            }).CallDeferred();
        }

        private void Reset()
        {
            _lastHealth = _player.Health;
            _stateMachine.ChangeState<Resting>();
        }

        public override void _Process(double delta)
        {
            if (_lastHealth != _player.Health)
            {
                _lastHealth = _player.Health;
                _stateMachine.ChangeState<Draining>();
            }
        }

        private void UpdatePosition(float t)
        {
            ProgressBar.AnchorLeft = Mathf.Lerp(RestPoint.AnchorLeft, CenterPoint.AnchorLeft, t);
            ProgressBar.AnchorRight = Mathf.Lerp(RestPoint.AnchorRight, CenterPoint.AnchorRight, t);
            ProgressBar.AnchorTop = Mathf.Lerp(RestPoint.AnchorTop, CenterPoint.AnchorTop, t);
            ProgressBar.AnchorBottom = Mathf.Lerp(RestPoint.AnchorBottom, CenterPoint.AnchorBottom, t);

            ProgressBar.OffsetLeft = Mathf.Lerp(RestPoint.OffsetLeft, CenterPoint.OffsetLeft, t);
            ProgressBar.OffsetRight = Mathf.Lerp(RestPoint.OffsetRight, CenterPoint.OffsetRight, t);
            OffsetTop = Mathf.Lerp(RestPoint.OffsetTop, CenterPoint.OffsetTop, t);
            OffsetBottom = Mathf.Lerp(RestPoint.OffsetBottom, CenterPoint.OffsetBottom, t);
        }

        private class Resting : State<HealthDisplay>
        {
            public override void OnStateEntered()
            {
                Self.UpdatePosition(0);
                Self.ProgressBar.Value = Self._player.Health;
            }
        }

        private class Draining : State<HealthDisplay>
        {
            private const double DrainDelay = 0.3;
            private const double DrainDuration = 0.125;
            private const double HangDuration = 0.5;
            private const double ReturnDuration = 0.5;

            private Coroutine _coroutine;

            public override void OnStateEntered()
            {
                _coroutine = new Coroutine(CoroutineExecutor());
                Self.UpdatePosition(1);
            }

            public override void OnStateExited()
            {
                Self.UpdatePosition(0);
                Self.ProgressBar.Scale = Vector2.One;
            }

            public override void _Process(double delta)
            {
                _coroutine.Tick(delta);
            }

            private IEnumerator<YieldInstruction> CoroutineExecutor()
            {
                yield return Coroutine.WaitFor(Pulse());
                yield return Coroutine.WaitFor(Drain());
                yield return Coroutine.WaitSeconds(HangDuration);
                yield return Coroutine.WaitFor(Return());

                ChangeState<Resting>();
            }

            private IEnumerator<YieldInstruction> Pulse()
            {
                var maxScale = Vector2.One * 1.25f;
                var minScale = Vector2.One;

                for (double timer = 0; timer <= DrainDelay; timer += Self.GetProcessDeltaTime())
                {
                    double t = Math.Min(timer / DrainDelay, 1.0);
                    t = Mathf.PingPong(t * 2, 1.0);
                    Self.ProgressBar.Scale = minScale.Lerp(maxScale, (float)t);

                    yield return default;
                }
            }

            private IEnumerator<YieldInstruction> Drain()
            {
                double startValue = Self.ProgressBar.Value;

                for (double timer = 0; timer <= DrainDuration; timer += Self.GetProcessDeltaTime())
                {
                    double targetValue = Self._player.Health;
                    double t = Math.Min(timer / DrainDuration, 1.0);
                    t *= t;

                    Self.ProgressBar.Value = Mathf.Lerp(startValue, targetValue, t);
                    yield return default;
                }

                Self.ProgressBar.Value = Self._player.Health;
            }

            private IEnumerator<YieldInstruction> Return()
            {
                for (double timer = 0; timer <= ReturnDuration; timer += Self.GetProcessDeltaTime())
                {
                    double t = 1.0 - Math.Min(timer / ReturnDuration, 1.0);
                    t *= t;

                    Self.UpdatePosition((float)t);
                    yield return default;
                }
            }
        }
    }
}
