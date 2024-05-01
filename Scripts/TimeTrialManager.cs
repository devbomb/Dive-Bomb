using System;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialManager : Node
    {
        public bool IsTimeTrialMode => Mode != TimeTrialMode.None;
        public bool IsTimerRunning {get; private set;} = false;

        public TimeTrialMode Mode {get; private set;} = TimeTrialMode.None;
        public enum TimeTrialMode
        {
            None,
            AnyPercent
        }

        public double Timer {get; private set;}

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Restart;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsTimerRunning && IsTimeTrialMode)
                Timer += delta;
        }

        public void Start(TimeTrialMode mode)
        {
            Mode = mode;
            Restart();
        }

        public void Restart()
        {
            Timer = 0;
            IsTimerRunning = true;
            MapTransitionManager.Instance.RespawnPlayerAfterDeath();
        }

        public void Finish()
        {
            IsTimerRunning = false;
        }
    }
}