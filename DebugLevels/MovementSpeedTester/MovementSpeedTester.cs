using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class MovementSpeedTester : Node
    {
        public const float GoalDistance = 200;

        [Export] public Player Player;
        [Export] public Label TimerLabel;
        [Export] public Label DistanceLabel;
        [Export] public ProgressBar ProgressBar;

        [Export] public Slider JumpDelayFramesSlider;
        [Export] public Slider DiveDelayFramesSlider;

        private PhysicsTicks _timer = 0;
        private Coroutine _coroutine = null;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        private void Reset()
        {
            _timer = 0;
            _coroutine = null;

            ProgressBar.Value = 0;
            DistanceLabel.Text = "0";
            TimerLabel.Text = "--";
        }

        public override void _PhysicsProcess(double delta)
        {
            if (!_coroutine?.Done ?? false)
            {
                _timer++;
                _coroutine.Tick(delta);
                float dist = PlayerTravelDistance();

                TimerLabel.Text = _timer.FormatStopwatch();
                DistanceLabel.Text = dist.ToString("#.##");

                ProgressBar.MaxValue = GoalDistance;
                ProgressBar.MinValue = 0;
                ProgressBar.Value = dist;
            }

            if (PlayerTravelDistance() >= GoalDistance)
                _coroutine = null;
        }

        public void StartWalkTest()
        {
            SignalBus.Instance.EmitLevelReset();
            _coroutine = new Coroutine(Test());

            IEnumerator<YieldInstruction> Test()
            {
                while (true)
                {
                    Input.ActionPress("LeftStickUp");
                    yield return default;
                }
            }
        }

        public void StartRollTest()
        {
            SignalBus.Instance.EmitLevelReset();
            _coroutine = new Coroutine(Test());

            IEnumerator<YieldInstruction> Test()
            {
                while (true)
                {
                    Input.ActionPress("LeftStickUp");

                    if (Player.CurrentState is not PlayerRollState)
                    {
                        PressButton("Roll");
                    }

                    yield return default;
                }
            }
        }

        public void StartDiveOnlyTest()
        {
            SignalBus.Instance.EmitLevelReset();
            _coroutine = new Coroutine(Test());

            IEnumerator<YieldInstruction> Test()
            {
                while (true)
                {
                    Input.ActionPress("LeftStickUp");

                    if (Player.IsOnFloor())
                    {
                        PressButton("Jump");
                    }
                    else
                    {
                        PressButton("Roll");
                    }

                    yield return default;
                }
            }
        }

        public void StartJumpDiveRollTest()
        {
            SignalBus.Instance.EmitLevelReset();
            _coroutine = new Coroutine(Test());

            IEnumerator<YieldInstruction> Test()
            {
                while (true)
                {
                    Input.ActionPress("LeftStickUp");

                    PressButton("Jump");
                    yield return Coroutine.WaitTicks((PhysicsTicks)DiveDelayFramesSlider.Value);

                    PressButton("Roll");
                    yield return Coroutine.WaitFor(StateToBe<PlayerRollState>());
                    yield return Coroutine.WaitTicks((PhysicsTicks)JumpDelayFramesSlider.Value);
                }
            }
        }

        private IEnumerator<YieldInstruction> StateToBe<TPlayerState>()
        {
            while (Player.CurrentState is not TPlayerState)
                yield return default;
        }

        private void PressButton(string action)
        {
            var ev = new InputEventAction
            {
                Action = action,
                Pressed = true
            };

            Input.ParseInputEvent(ev);
        }

        private float PlayerTravelDistance() => Player
            .GlobalPosition
            .Flattened()
            .Length();
    }
}