using System;
using Godot;

namespace FastDragon
{
    public partial class KrackenSplashTentacle : Node3D
    {
        [Export] public PackedScene StraightWavePrefab;
        [Export] public Node3D WaveSpawn;

        [Export] public float WaveHeight = 1;
        [Export] public float WaveStartWidth = 8;
        [Export] public float WaveEndWidth = 16;
        [Export] public float WaveDistance = 16;
        [Export] public float WaveDuration = 2;

        [Signal] public delegate void DamagedPlayerEventHandler();

        private AnimationPlayer _animator => GetNode<AnimationPlayer>("%AnimationPlayer");

        private Transform3D _lockedWaveSpawn;

        public override void _Ready()
        {
            SignalBus.Instance.LevelReset += Reset;
            Reset();
        }

        public void Reset()
        {
            _animator.ClearQueue();
            _animator.Play("Submerged");
        }

        public void Surface()
        {
            _animator.ClearQueue();
            _animator.Play("Surface");
            _animator.Queue("Idle");
        }

        public void StartSplash()
        {
            _animator.ClearQueue();
            _animator.Play("Splash", 0.1f);
            _animator.Queue("SplashRecover");
            _animator.Queue("Idle");

            // HACK: Determine where the wave will spawn _now_, instead of when
            // the wave actually spawns.
            //
            // This way, the spawn point won't suddenly shift if the player
            // damages the boss in the middle of a swing.  That would be very
            // unfortunate for them.
            _lockedWaveSpawn = WaveSpawn.GlobalTransform;
        }

        public void Submerge()
        {
            _animator.ClearQueue();
            _animator.Play("Submerge", 0.1f);
            _animator.Queue("Submerged");
        }

        public StraightWave SpawnWaveAttack()
        {
            var wave = StraightWavePrefab.Instantiate<StraightWave>();
            GetTree().CurrentScene.AddChild(wave);
            wave.GlobalTransform = _lockedWaveSpawn;

            wave.Radius = WaveHeight;
            wave.StartWidth = WaveStartWidth;
            wave.EndWidth = WaveEndWidth;
            wave.Distance = WaveDistance;
            wave.Duration = WaveDuration;
            wave.DamagedPlayer += () => EmitSignal(nameof(DamagedPlayer));

            return wave;
        }
    }
}