using Godot;

namespace FastDragon
{
    [GlobalClass]
    public partial class BackgroundMusicPlayer : AudioStreamPlayer
    {
        public override void _Ready()
        {
            SignalBus.Instance.ExitReached += Stop;
            SignalBus.Instance.LevelReset += Reset;

            Reset();
        }

        private void Reset()
        {
            if (!Playing)
                Play();
        }
    }
}