#nullable enable
using Godot;

namespace FastDragon
{
    public partial class BackgroundMusicPlayer : Node
    {
        /// <summary>
        ///     The song that plays when you first enter the level.
        ///     For most levels, this is the _only_ music that will play in the
        ///     level.
        /// </summary>
        [Export] public required BackgroundSong DefaultSong;

        /// <summary>
        ///     How long to wait before starting the song after a level reset.
        /// </summary>
        [Export] public double StartDelaySeconds = 0.25;

        [ExportCategory("Internal")]
        [Export] public required AudioStreamPlayer AudioPlayer;

        public BackgroundSong CurrentSong => _songOverride ?? DefaultSong;
        private BackgroundSong? _songOverride = null;

        private readonly StateMachine _stateMachine = new();

        public BackgroundMusicPlayer()
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
            RestartSong();
        }

        public void RestartSong()
        {
            _stateMachine.ChangeState<DelayingStart>();
        }

        public void Stop()
        {
            _stateMachine.ChangeState<Stopped>();
        }

        public void OverrideSong(BackgroundSong song)
        {
            _songOverride = song;

            if (_stateMachine.CurrentState is Playing)
                RestartSong();
        }

        public void RemoveSongOverride()
        {
            _songOverride = null;

            if (_stateMachine.CurrentState is Playing)
                RestartSong();
        }

        private class DelayingStart : State<BackgroundMusicPlayer>
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

        private class Playing : State<BackgroundMusicPlayer>
        {
            public override void OnStateEntered()
            {
                Self.AudioPlayer.Stream = Self.CurrentSong.Music;
                Self.AudioPlayer.VolumeLinear = Self.CurrentSong.VolumeMultiplierLinear;
                Self.AudioPlayer.Play();
            }
        }

        private class Stopped : State<BackgroundMusicPlayer>
        {
            public override void OnStateEntered()
            {
                Self.AudioPlayer.Stop();
            }
        }
    }
}