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
            _chosenPowerOrbs = _rng.Shuffled(PowerOrbs)
                .Take(4)
                .ToArray();

            foreach (var orb in _chosenPowerOrbs)
                orb.Reveal();
        }

        private void RandomizeSpawnPoint()
        {
            _currentSpawnPos = _rng.PickFrom(SpawnPoints).GlobalTransform;
        }

        private class Submerging : State<SeamonsterBoss>
        {
            public override void OnStateEntered()
            {
                Self.HidePowerOrbs();
                Self.PlayAnimation("Submerge");
                Self._leftSplashTentacle.Submerge();
                Self._rightSplashTentacle.Submerge();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (Self.CurrentAnimation() != "Submerge")
                {
                    Self.RandomizeSpawnPoint();
                    ChangeState<Submerged>();
                }
            }
        }

        private class Submerged : State<SeamonsterBoss>
        {
            private float _timer;

            public override void OnStateEntered()
            {
                Self.Visible = false;

                Self.GlobalTransform = Self._currentSpawnPos;
                Self.GlobalPosition += Vector3.Up * Self.SubmergeDepth;
                Self.ResetPhysicsInterpolation();

                _timer = Self.SubmergedDuration;
            }

            public override void OnStateExited()
            {
                Self.Visible = true;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<Surfacing>();
            }
        }

        private class Surfacing : State<SeamonsterBoss>
        {
            private float _timer;

            public override void OnStateEntered()
            {
                Self.GlobalTransform = Self._currentSpawnPos;
                Self.ResetPhysicsInterpolation();

                _timer = Self.SurfacingDuration;

                Self.RevealPowerOrbs();
                Self.PlayAnimation("Surface");
                Self._leftSplashTentacle.Surface();
                Self._rightSplashTentacle.Surface();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<WavesAttack>();
            }
        }

        private class Vulnerable : State<SeamonsterBoss>
        {
            private float _timer;

            public override void OnStateEntered()
            {
                Self.ShowWeakPoint(true);
                Self._weakPoint.Broken += OnDamagedByPlayer;

                _timer = Self.VulnerableDuration;
            }

            public override void OnStateExited()
            {
                Self.ShowWeakPoint(false);
                Self._weakPoint.Broken -= OnDamagedByPlayer;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer -= (float)deltaD;

                if (_timer <= 0)
                    ChangeState<Submerging>();
            }

            private void OnDamagedByPlayer()
            {
                Self._health.Damage();

                GetTree().FindNode<Player>().ChangeState<PlayerBonkState>();
                ChangeState<Damaged>();
            }
        }

        private class Damaged : State<SeamonsterBoss>
        {
            private float _timer;
            private Vector3 _startPos;
            private Vector3 _endPos;

            public override void OnStateEntered()
            {
                _timer = 0;

                _startPos = Self.GlobalPosition;
                _endPos = _startPos - (Self.GlobalForward() * Self.HurtKnockbackDistance);
                Self.PlayAnimation("Damaged");

                if (Self._health.CurrentHealth <= 0)
                {
                    Self._leftSplashTentacle.Submerge();
                    Self._rightSplashTentacle.Submerge();
                }
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;
                float t = _timer / Self.HurtKnockbackDuration;
                t = Mathf.Sqrt(t);
                t = Mathf.Min(t * 2, 1);

                Self.GlobalPosition = _startPos.Lerp(_endPos, t);

                if (_timer >= Self.HurtKnockbackDuration)
                {
                    if (Self._health.CurrentHealth > 0)
                        ChangeState<AcidSplashesSubmerging>();
                    else
                        ChangeState<Dying>();
                }
            }
        }

        private class Dying : State<SeamonsterBoss>
        {
            public override void OnStateEntered()
            {
                Self.UseCameraAngle(Self._deathAnimationCameraPos.GlobalTransform);
                Self.PlayAnimation("Dying");

                // Clear out all of the permanent acid splashes
                var splashes = GetTree().Root.EnumerateDescendantsOfType<FallingAcidBlob>();
                foreach (var splash in splashes)
                    splash.QueueFree();

                // Don't let the player run around and die during the cutscene
                GetTree().Paused = true;
                Self.ProcessMode = ProcessModeEnum.Always;
                GetTree().FindNode<PlayerCamera>().ProcessMode = ProcessModeEnum.Always;
            }

            public override void OnStateExited()
            {
                GetTree().Paused = false;
                Self.ProcessMode = ProcessModeEnum.Inherit;
                GetTree().FindNode<PlayerCamera>().ProcessMode = ProcessModeEnum.Inherit;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                if (Self.CurrentAnimation() != "Dying")
                    ChangeState<Dead>();
            }
        }

        private class Dead : State<SeamonsterBoss>
        {
            public override void OnStateEntered()
            {
                Self.UseBossCameraAngle();
                Self.ReturnHomeVortex.Reveal();

                Self._bossHud.Visible = false;
            }

            public override void OnStateExited()
            {
                Self._bossHud.Visible = true;
            }
        }

        private class Laughing : State<SeamonsterBoss>
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = Self.LaughingDuration;
                Self.PlayAnimation("Laugh", true);
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
                    ChangeState<Submerging>();
                    return;
                }
            }
        }
    }
}