using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss
    {
        [ExportGroup("Attacks/Wave")]
        [Export] public PackedScene StraightWavePrefab;

        [Export] public float WaveInterval = 1.67f;
        [Export] public int WaveCount = 3;

        private partial class WavesAttack : SeamonsterBossState
        {
            private int _wavesRemaining;
            private float _timer;

            public override void OnStateEntered()
            {
                _wavesRemaining = _self.WaveCount;
                _timer = _self.WaveInterval;

                _self._leftSplashTentacle.DamagedPlayer += OnDamagedPlayer;
                _self._rightSplashTentacle.DamagedPlayer += OnDamagedPlayer;
            }

            public override void OnStateExited()
            {
                _self._leftSplashTentacle.DamagedPlayer -= OnDamagedPlayer;
                _self._rightSplashTentacle.DamagedPlayer -= OnDamagedPlayer;
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

                    var tentacle = _wavesRemaining % 2 == 0
                        ? _self._leftSplashTentacle
                        : _self._rightSplashTentacle;

                    tentacle.StartSplash();
                    _self.PlayAnimation("Swing", travel: true);
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