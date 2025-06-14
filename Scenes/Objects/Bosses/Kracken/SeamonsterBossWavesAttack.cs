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
                _wavesRemaining = Self.WaveCount;
                _timer = Self.WaveInterval;

                Self._leftSplashTentacle.DamagedPlayer += OnDamagedPlayer;
                Self._rightSplashTentacle.DamagedPlayer += OnDamagedPlayer;
            }

            public override void OnStateExited()
            {
                Self._leftSplashTentacle.DamagedPlayer -= OnDamagedPlayer;
                Self._rightSplashTentacle.DamagedPlayer -= OnDamagedPlayer;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (Self.AllPowerOrbsBroken())
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
                    _timer += Self.WaveInterval;

                    var tentacle = _wavesRemaining % 2 == 0
                        ? Self._leftSplashTentacle
                        : Self._rightSplashTentacle;

                    tentacle.StartSplash();
                    Self.PlayAnimation("Swing", travel: true);
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