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
            if (_timer > 0)
            {
                Engine.TimeScale = HitStopTimeScale;
                _timer -= delta / HitStopTimeScale;

                if (_timer <= 0)
                {
                    Engine.TimeScale = 1;
                }
            }
        }

        public void StopFor(double seconds)
        {
            _timer = seconds;
        }
    }
}