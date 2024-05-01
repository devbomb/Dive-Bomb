using System;
using Godot;

namespace FastDragon
{
    public partial class TimeTrialManager : Node
    {
        public static TimeTrialManager Instance {get; private set;}

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
            Instance = this;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (IsTimerRunning && IsTimeTrialMode)
                Timer += delta;
        }

        public void Start(TimeTrialMode mode)
        {
            if (IsTimeTrialMode)
                throw new Exception("Already in time trial mode");

            if (mode == TimeTrialMode.None)
                throw new Exception("Cannot start a time trial in \"none\" mode");

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