using Godot;

namespace FastDragon
{
    public partial class HitStopManager : Node
    {
        private const float HitStopTimeScale = 0.1f;

        public static HitStopManager Instance { get; private set; }
        private double _timer;

        public override void _Ready()
        {
            Instance = this;
            ProcessMode = ProcessModeEnum.Always;
        }

        public override void _PhysicsProcess(double delta)
        {
            Engine.TimeScale = _timer > 0
                ? HitStopTimeScale
                : 1;

            if (_timer > 0)
                _timer -= delta / HitStopTimeScale;
        }

        public void StopFor(double seconds)
        {
            _timer = seconds;
        }
    }
}