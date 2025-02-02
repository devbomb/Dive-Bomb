using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss
    {
        [ExportGroup("Attacks/Acid Splashes")]
        [Export] public PackedScene FallingAcidBlobPrefab;
        [Export] public Node3D AcidSplashesCameraPoint;
        [Export] public float AcidSplashesInterval = 0.5f;
        [Export] public int AcidSplashCount = 4;

        private partial class AcidSplashesSubmerging : SeamonsterBossState
        {
            public override void OnStateEntered()
            {
                _self.UseOverheadCameraAngle();
                _self.PlayAnimation("Submerge");
                _self._leftSplashTentacle.Submerge();
                _self._rightSplashTentacle.Submerge();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (_self.CurrentAnimation() != "Submerge")
                {
                    _self.RandomizeSpawnPoint();
                    ChangeState<AcidSplashesRaining>();
                }
            }
        }

        private partial class AcidSplashesRaining : SeamonsterBossState
        {
            private float _timer;
            private int _splashesRemaining;

            public override void OnStateEntered()
            {
                _timer = _self.AcidSplashesInterval;
                _splashesRemaining = _self.AcidSplashCount;
                SpawnSplash();
            }

            public override void OnStateExited()
            {
                _self.UseBossCameraAngle();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                {
                    if (_splashesRemaining > 0)
                    {
                        SpawnSplash();
                        _timer += _self.AcidSplashesInterval;
                    }
                    else
                    {
                        ChangeState<Submerged>();
                    }
                }
            }

            private void SpawnSplash()
            {
                _splashesRemaining--;

                var acidSplash = _self.FallingAcidBlobPrefab.Instantiate<FallingAcidBlob>();

                GetTree().CurrentScene.AddChild(acidSplash);

                acidSplash.GlobalPosition = GetTree()
                    .FindNode<Player>()
                    .GlobalPosition
                    .Flattened();

                // HACK: Make the last one permanent
                // TODO: Do this somewhere else?
                // TODO: Make sure it despawns when the boss dies.
                if (_splashesRemaining <= 0 && _self._health.CurrentPhase > 0)
                    acidSplash.Permanent = true;
            }
        }
    }
}