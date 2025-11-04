using System;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class MovementSpeedTester : Node
    {
        public const double GoalDistance = 100.0;

        [Export] public Player Player;

        private double _timer = 0;
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
            _timer += delta;
            _coroutine?.Tick(delta);
        }

        public void StartWalkTest()
        {
            SignalBus.Instance.EmitLevelReset();
            _coroutine = new Coroutine(Test());

            IEnumerator<YieldInstruction> Test()
            {
                while (PlayerTravelDistance() < GoalDistance)
                {
                    Input.ActionPress("LeftStickUp", 1);
                    yield return default;
                }

                GD.Print(_timer);
            }
        }

        private float PlayerTravelDistance() => Player
            .GlobalPosition
            .Flattened()
            .Length();
    }
}