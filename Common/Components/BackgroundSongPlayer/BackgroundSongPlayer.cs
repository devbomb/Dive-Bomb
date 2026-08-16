using Godot;

namespace FastDragon
{
    public partial class BackgroundSongPlayer : Node
    {
        /// <summary>
        ///     The song that plays when you first enter the level.
        ///     For most levels, this is the _only_ music that will play in the
        ///     level.
        /// </summary>
        [Export] public BackgroundSong DefaultSong;

        /// <summary>
        ///     How long to wait before starting the song after a level reset.
        /// </summary>
        [Export] public double StartDelaySeconds = 0.25;

        [ExportCategory("Internal")]
        [Export] public AudioStreamPlayer AudioPlayer;

        public BackgroundSong CurrentSong => DefaultSong;

        private readonly StateMachine _stateMachine = new();

        public BackgroundSongPlayer()
        {
            AddChild(_stateMachine);
        }

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            SignalBus.Instance.ExitReached += Stop;
            Reset();
        }

        private void Reset()
        {
            _stateMachine.ChangeState<DelayingStart>();
        }

        public void Stop()
        {
            _stateMachine.ChangeState<Stopped>();
        }

        private class DelayingStart : State<BackgroundSongPlayer>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                _timer = Self.StartDelaySeconds;
                Self.AudioPlayer.Stop();
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                    ChangeState<Playing>();
            }
        }

        private class Playing : State<BackgroundSongPlayer>
        {
            public override void OnStateEntered()
            {
                Self.AudioPlayer.Stream = Self.CurrentSong.Music;
                Self.AudioPlayer.VolumeLinear = Self.CurrentSong.VolumeMultiplierLinear;
                Self.AudioPlayer.Play();
            }
        }

        private class Stopped : State<BackgroundSongPlayer>
        {
            public override void OnStateEntered()
            {
                Self.AudioPlayer.Stop();
            }
        }
    }
}