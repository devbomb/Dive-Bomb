using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss
    {
        [ExportGroup("Attacks/Acid Splashes")]
        [Export] public PackedScene FallingAcidBlobPrefab;
        [Export] public Node3D AcidSplashesCameraPoint;
        [Export] public double AcidSplashesInterval = 0.5f;
        [Export] public int AcidSplashCount = 4;

        private class AcidSplashesSubmerging : State<SeamonsterBoss>
        {
            private double _timer;

            public override void OnStateEntered()
            {
                Self.UseOverheadCameraAngle();
                Self._leftSplashTentacle.Submerge();
                Self._rightSplashTentacle.Submerge();

                _timer = Self.PlayAnimGetLength("Submerge");
            }

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

                if (_timer <= 0)
                {
                    Self.RandomizeSpawnPoint();
                    ChangeState<AcidSplashesRaining>();
                }
            }
        }

        private class AcidSplashesRaining : State<SeamonsterBoss>
        {
            private double _timer;
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

            public override void _PhysicsProcess(double delta)
            {
                _timer -= delta;

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