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
            private float _timer;
            private Transform3D _initialPos;
            private Transform3D _targetPos;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPos = _self.GlobalTransform;
                _targetPos = _initialPos.Translated(Vector3.Up * _self.SubmergeDepth);

                _self.UseOverheadCameraAngle();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                float t = Mathf.Min(_timer / _self.SubmergingDuration, 1);
                _self.GlobalTransform = _initialPos.InterpolateWith(_targetPos, t);

                if (_timer >= _self.SubmergingDuration)
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
            }
        }
    }
}