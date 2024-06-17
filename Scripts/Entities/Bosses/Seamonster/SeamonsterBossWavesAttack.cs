using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss
    {
        [ExportGroup("Attacks/Wave")]
        [Export] public PackedScene StraightWavePrefab;
        [Export] public float WaveHeight = 1;
        [Export] public float WaveStartWidth = 8;
        [Export] public float WaveEndWidth = 16;
        [Export] public float WaveDistance = 16;
        [Export] public float WaveDuration = 2;
        [Export] public float WaveInterval = 1.67f;
        [Export] public int WaveCount = 3;

        private Node3D _leftWaveSpawn => GetNode<Node3D>("%LeftWaveSpawn");
        private Node3D _rightWaveSpawn => GetNode<Node3D>("%RightWaveSpawn");

        private StraightWave SpawnWaveAttack(Node3D spawnPoint)
        {
            var wave = StraightWavePrefab.Instantiate<StraightWave>();
            GetTree().CurrentScene.AddChild(wave);
            wave.GlobalTransform = spawnPoint.GlobalTransform;

            wave.Radius = WaveHeight;
            wave.StartWidth = WaveStartWidth;
            wave.EndWidth = WaveEndWidth;
            wave.Distance = WaveDistance;
            wave.Duration = WaveDuration;

            return wave;
        }

        private partial class WavesAttack : SeamonsterBossState
        {
            private int _wavesRemaining;
            private float _timer;

            public override void OnStateEntered()
            {
                _wavesRemaining = _self.WaveCount;
                _timer = _self.WaveInterval;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_self.AllPowerOrbsBroken())
                {
                    ChangeState<Vulnerable>();
                    return;
                }

                if (_timer <= 0)
                {
                    if (_wavesRemaining <= 0)
                    {
                        ChangeState<Submerging>();
                        return;
                    }

                    _wavesRemaining--;
                    _timer += _self.WaveInterval;

                    var spawnPoint = _wavesRemaining % 2 == 0
                        ? _self._leftWaveSpawn
                        : _self._rightWaveSpawn;
                    var wave = _self.SpawnWaveAttack(spawnPoint);
                    wave.DamagedPlayer += OnDamagedPlayer;
                }
            }

            private void OnDamagedPlayer()
            {
                if (IsCurrent)
                    ChangeState<Laughing>();
            }
        }

    }
}