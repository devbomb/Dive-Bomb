using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class BackgroundMusicPlayer : AudioStreamPlayer
    {
        [Export] public double StartDelaySeconds = 0.25;
        private double _startDelayTimer;
        private bool _delayingStart = false;

        public override void _Ready()
        {
            SignalBus.Instance.ExitReached += Stop;
            SignalBus.Instance.LevelReset += Reset;

            Reset();

            // HACK: Delay starting the music to avoid a bug where the music
            // briefly starts during the fly-in animation when it's not supposed
            // to.
            //
            // TODO: Find a more elegant way to solve this.
            Stop();
            _delayingStart = true;
            _startDelayTimer = StartDelaySeconds;
        }

        private void Reset()
        {
            // No need to delay if we're already in the level.
            // Delaying in that case could clobber other scripts that try to
            // change the music depending on a story flag (EG: TutorialStoryManager)
            _delayingStart = false;
        }

        public override void _PhysicsProcess(double delta)
        {
            if (_delayingStart)
            {
                _startDelayTimer -= delta;
                if (_startDelayTimer <= 0)
                {
                    _delayingStart = false;
                    Play();
                }
            }
        }
    }
}