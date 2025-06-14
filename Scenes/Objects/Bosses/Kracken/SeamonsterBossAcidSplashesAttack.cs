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
                Self.UseOverheadCameraAngle();
                Self.PlayAnimation("Submerge");
                Self._leftSplashTentacle.Submerge();
                Self._rightSplashTentacle.Submerge();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (Self.CurrentAnimation() != "Submerge")
                {
                    Self.RandomizeSpawnPoint();
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
                _timer = Self.AcidSplashesInterval;
                _splashesRemaining = Self.AcidSplashCount;
                SpawnSplash();
            }

            public override void OnStateExited()
            {
                Self.UseBossCameraAngle();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                {
                    if (_splashesRemaining > 0)
                    {
                        SpawnSplash();
                        _timer += Self.AcidSplashesInterval;
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

                var acidSplash = Self.FallingAcidBlobPrefab.Instantiate<FallingAcidBlob>();

                GetTree().CurrentScene.AddChild(acidSplash);

                acidSplash.GlobalPosition = GetTree()
                    .FindNode<Player>()
                    .GlobalPosition
                    .Flattened();

                // Make the last one permanent.
                // This is an easy way to increase the tension as the fight
                // goes on.
                //
                // It'll be deleted at the start of the Dying state.
                if (_splashesRemaining <= 0 && Self._health.CurrentPhase > 0)
                    acidSplash.Permanent = true;
            }
        }
    }
}