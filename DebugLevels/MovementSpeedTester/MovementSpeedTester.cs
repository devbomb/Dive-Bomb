using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class MovementSpeedTester : Node
    {
        public const float GoalDistance = 100;

        [Export] public Player Player;
        [Export] public Label TimerLabel;
        [Export] public Label DistanceLabel;
        [Export] public ProgressBar ProgressBar;

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
        }

        public void StartWalkTest()
        {
            SignalBus.Instance.EmitLevelReset();
            _coroutine = new Coroutine(Test());

            IEnumerator<YieldInstruction> Test()
            {
                while (PlayerTravelDistance() < GoalDistance)
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
                while (PlayerTravelDistance() < GoalDistance)
                {
                    Input.ActionPress("LeftStickUp");

                    if (Player.CurrentState is not PlayerRollState)
                    {
                        var ev = new InputEventAction
                        {
                            Action = "Roll",
                            Pressed = true
                        };
                        Input.ParseInputEvent(ev);
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
                while (PlayerTravelDistance() < GoalDistance)
                {
                    Input.ActionPress("LeftStickUp");

                    if (Player.IsOnFloor())
                    {
                        var ev = new InputEventAction
                        {
                            Action = "Jump",
                            Pressed = true
                        };
                        Input.ParseInputEvent(ev);
                    }
                    else
                    {
                        var ev = new InputEventAction
                        {
                            Action = "Roll",
                            Pressed = true
                        };
                        Input.ParseInputEvent(ev);
                    }

                    yield return default;
                }
            }
        }

        private float PlayerTravelDistance() => Player
            .GlobalPosition
            .Flattened()
            .Length();
    }
}