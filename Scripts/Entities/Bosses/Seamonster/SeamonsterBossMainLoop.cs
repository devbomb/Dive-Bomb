using System;
using System.Linq;
using System.Collections.Generic;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss
    {
        [ExportGroup("Main Loop")]
        [Export] public Node3D InitialSpawnPoint;
        [Export] public Node3D CameraFixPoint;
        [Export] public Node3D[] SpawnPoints = new Node3D[0];
        [Export] public PowerOrb[] PowerOrbs = new PowerOrb[0];

        [ExportGroup("Main Loop/Submerge")]
        [Export] public float SubmergedDuration = 1;
        [Export] public float SubmergeDepth = -14;

        [ExportGroup("Main Loop/Surface")]
        [Export] public float SurfacingDuration = 0.5f;

        [ExportGroup("Main Loop/Laugh")]
        [Export] public float LaughingDuration = 3;

        [ExportGroup("Main Loop/Vulnerable and Hurt")]
        [Export] public float VulnerableDuration = 3;
        [Export] public float HurtKnockbackDistance = 10;
        [Export] public float HurtKnockbackDuration = 1.5f;

        private Node3D _deathAnimationCameraPos => GetNode<Node3D>("%DeathAnimationCameraPos");

        private bool AllPowerOrbsBroken()
        {
            return _chosenPowerOrbs.Length > 0 && _chosenPowerOrbs.All(o => o.IsBroken);
        }

        private void HidePowerOrbs()
        {
            foreach (var orb in PowerOrbs)
                orb.SetHidden();
        }

        private void RevealPowerOrbs()
        {
            _chosenPowerOrbs = _rng.Shuffle(PowerOrbs)
                .Take(4)
                .ToArray();

            foreach (var orb in _chosenPowerOrbs)
                orb.Reveal();
        }

        private void RandomizeSpawnPoint()
        {
            _currentSpawnPos = _rng.PickFrom(SpawnPoints).GlobalTransform;
        }

        private partial class Submerging : SeamonsterBossState
        {
            public override void OnStateEntered()
            {
                _self.HidePowerOrbs();
                _self.PlayAnimation("Submerge");
                _self._leftSplashTentacle.Submerge();
                _self._rightSplashTentacle.Submerge();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (_self.CurrentAnimation() != "Submerge")
                {
                    _self.RandomizeSpawnPoint();
                    ChangeState<Submerged>();
                }
            }
        }

        private partial class Submerged : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _self.Visible = false;

                _self.GlobalTransform = _self._currentSpawnPos;
                _self.GlobalPosition += Vector3.Up * _self.SubmergeDepth;
                _self.ResetPhysicsInterpolation();

                _timer = _self.SubmergedDuration;
            }

            public override void OnStateExited()
            {
                _self.Visible = true;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<Surfacing>();
            }
        }

        private partial class Surfacing : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _self.GlobalTransform = _self._currentSpawnPos;
                _self.ResetPhysicsInterpolation();

                _timer = _self.SurfacingDuration;

                _self.RevealPowerOrbs();
                _self.PlayAnimation("Surface");
                _self._leftSplashTentacle.Surface();
                _self._rightSplashTentacle.Surface();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<WavesAttack>();
            }
        }

        private partial class Vulnerable : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _self.ShowWeakPoint(true);
                _self._weakPoint.Broken += OnDamagedByPlayer;

                _timer = _self.VulnerableDuration;
            }

            public override void OnStateExited()
            {
                _self.ShowWeakPoint(false);
                _self._weakPoint.Broken -= OnDamagedByPlayer;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<Submerging>();
            }

            private void OnDamagedByPlayer()
            {
                _self._health.Damage();

                GetTree().FindNode<Player>().ChangeState<PlayerBonkState>();

                if (_self._health.CurrentHealth > 0)
                    ChangeState<Damaged>();
                else
                    ChangeState<Dying>();
            }
        }

        private partial class Damaged : SeamonsterBossState
        {
            private float _timer;
            private Vector3 _startPos;
            private Vector3 _endPos;

            public override void OnStateEntered()
            {
                _timer = 0;

                _startPos = _self.GlobalPosition;
                _endPos = _startPos - (_self.GlobalForward() * _self.HurtKnockbackDistance);
                _self.PlayAnimation("Damaged");
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                float t = _timer / _self.HurtKnockbackDuration;
                t = Mathf.Sqrt(t);
                t = Mathf.Min(t * 2, 1);

                _self.GlobalPosition = _startPos.Lerp(_endPos, t);

                if (_timer >= _self.HurtKnockbackDuration)
                    ChangeState<AcidSplashesSubmerging>();
            }
        }

        private partial class Dying : SeamonsterBossState
        {
            private float _timer;
            private Vector3 _startPos;
            private Vector3 _endPos;

            private bool _claimedCamera;

            public override void OnStateEntered()
            {
                _timer = 0;
                _claimedCamera = false;

                _startPos = _self.GlobalPosition;
                _endPos = _startPos - (_self.GlobalForward() * _self.HurtKnockbackDistance);

                _self._leftSplashTentacle.Submerge();
                _self._rightSplashTentacle.Submerge();
                _self.PlayAnimation("Dying");

                // Clear out all of the permanent acid splashes
                var splashes = GetTree().Root.EnumerateDescendantsOfType<FallingAcidBlob>();
                foreach (var splash in splashes)
                    splash.QueueFree();

                // Don't let the player run around and die during the cutscene
                GetTree().Paused = true;
                _self.ProcessMode = ProcessModeEnum.Always;
                GetTree().FindNode<PlayerCamera>().ProcessMode = ProcessModeEnum.Always;
            }

            public override void OnStateExited()
            {
                GetTree().Paused = false;
                _self.ProcessMode = ProcessModeEnum.Inherit;
                GetTree().FindNode<PlayerCamera>().ProcessMode = ProcessModeEnum.Inherit;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                float t = _timer / _self.HurtKnockbackDuration;
                t = Mathf.Sqrt(t);
                t = Mathf.Min(t * 2, 1);

                _self.GlobalPosition = _startPos.Lerp(_endPos, t);

                if (_timer >= _self.HurtKnockbackDuration && !_claimedCamera)
                {
                    _self.UseCameraAngle(_self._deathAnimationCameraPos.GlobalTransform);
                    _claimedCamera = true;
                }

                if (_self.CurrentAnimation() != "Dying")
                    ChangeState<Dead>();
            }
        }

        private partial class Dead : SeamonsterBossState
        {
            public override void OnStateEntered()
            {
                _self.UseBossCameraAngle();
                _self.ReturnHomeVortex.Reveal();
            }
        }

        private partial class Laughing : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = _self.LaughingDuration;
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
                    ChangeState<Submerging>();
                    return;
                }
            }
        }
    }
}