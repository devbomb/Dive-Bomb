using System;
using System.Linq;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss : CharacterBody3D
    {
        [ExportCategory("Timings")]
        [Export] public float SubmergedDuration = 1;
        [Export] public float SubmergingDuration = 0.5f;
        [Export] public float SurfacingDuration = 0.5f;
        [Export] public float ThickBeamDuration = 5f;
        [Export] public float VulnerableDuration = 3;
        [Export] public float LaughingDuration = 3;

        [ExportCategory("Points")]
        [Export] public float SubmergeDepth = -14;
        [Export] public Node3D InitialSpawnPoint;
        [Export] public Node3D CameraFixPoint;
        [Export] public Node3D[] SpawnPoints = new Node3D[0];
        [Export] public PowerOrb[] PowerOrbs = new PowerOrb[0];

        [ExportCategory("Attack Parameters")]
        [ExportGroup("Thick Beam")]
        [Export] public float ThickBeamRadius = 1;
        [Export] public float ThickBeamElongateSpeed = 40;
        [Export] public float ThickBeamTargetMoveSpeed = 2.5f;
        [Export] public float ThickBeamStartDelay = 0.5f;

        [ExportGroup("Wave")]
        [Export] public float WaveHeight = 1;
        [Export] public float WaveStartWidth = 8;
        [Export] public float WaveEndWidth = 16;
        [Export] public float WaveDistance = 16;
        [Export] public float WaveDuration = 2;

        [Export] public float WaveInterval = 1.67f;
        [Export] public int WaveCount = 3;

        [ExportCategory("Prefabs")]
        [ExportGroup("Prefabs")]
        [Export] public PackedScene StraightWavePrefab;

        private BreakableArea3D _weakPoint => GetNode<BreakableArea3D>("%WeakPoint");
        private ThickBeam _thickBeam => GetNode<ThickBeam>("%ThickBeam");
        private Node3D _straightWaveSpawn => GetNode<Node3D>("%StraightWaveSpawn");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(SeamonsterBossState));
        private readonly Random _rng = new Random();

        private Transform3D _currentSpawnPos;
        private PowerOrb[] _chosenPowerOrbs = new PowerOrb[0];


        public override void _Ready()
        {
            AddChild(_stateMachine);

            SignalBus.Instance.LevelReset += Respawn;

            // Defer respawning to ensure the player is ready, since we'll be
            // hijacking the camera.
            CallDeferred(nameof(Respawn));
        }

        public void Respawn()
        {
            _currentSpawnPos = InitialSpawnPoint.GlobalTransform;
            _stateMachine.ChangeState<Submerged>();

            // Hijack the camera
            GetTree().FindNode<Player>().Camera.FixPosition(CameraFixPoint.GlobalTransform);
        }

        private void RevealPowerOrbs()
        {
            _chosenPowerOrbs = _rng.Shuffle(PowerOrbs)
                .Take(4)
                .ToArray();

            foreach (var orb in _chosenPowerOrbs)
                orb.Reveal();
        }

        private void HidePowerOrbs()
        {
            foreach (var orb in PowerOrbs)
                orb.SetHidden();
        }

        private void ShowWeakPoint(bool shouldShow)
        {
            _weakPoint.Disabled = !shouldShow;
            _weakPoint.Visible = shouldShow;
        }

        private StraightWave SpawnWaveAttack()
        {
            var wave = StraightWavePrefab.Instantiate<StraightWave>();
            GetTree().CurrentScene.AddChild(wave);
            wave.GlobalTransform = _straightWaveSpawn.GlobalTransform;

            wave.Radius = WaveHeight;
            wave.StartWidth = WaveStartWidth;
            wave.EndWidth = WaveEndWidth;
            wave.Distance = WaveDistance;
            wave.Duration = WaveDuration;

            return wave;
        }

        private bool AllPowerOrbsBroken()
        {
            return _chosenPowerOrbs.Length > 0 && _chosenPowerOrbs.All(o => o.IsBroken);
        }

        private void RandomizeSpawnPoint()
        {
            _currentSpawnPos = _rng.PickFrom(SpawnPoints).GlobalTransform;
        }

        private abstract partial class SeamonsterBossState : State
        {
            protected SeamonsterBoss _self => _stateMachine.GetParent<SeamonsterBoss>();
        }

        private partial class Submerged : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _self.GlobalTransform = _self._currentSpawnPos;
                _self.GlobalPosition += Vector3.Up * _self.SubmergeDepth;
                _self.ResetPhysicsInterpolation();

                _timer = _self.SubmergedDuration;
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
            private Transform3D _initialPos;

            private float _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPos = _self.GlobalTransform;

                _self.RevealPowerOrbs();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                float t = Mathf.Min(_timer / _self.SurfacingDuration, 1);
                _self.GlobalTransform = _initialPos.InterpolateWith(_self._currentSpawnPos, t);

                if (_timer >= _self.SurfacingDuration)
                {
                    _self.GlobalTransform = _self._currentSpawnPos;
                    ChangeState<WavesAttack>();
                }

            }
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

                    var wave = _self.SpawnWaveAttack();
                    wave.DamagedPlayer += OnDamagedPlayer;
                }
            }

            private void OnDamagedPlayer()
            {
                if (IsCurrent)
                    ChangeState<Laughing>();
            }
        }

        private partial class ThickBeamAttack : SeamonsterBossState
        {
            private float _timer;

            public override void OnStateEntered()
            {
                _timer = 0;

                _self._thickBeam.DamagedPlayer += OnDealtDamageToPlayer;
                _self._thickBeam.TargetPos = PlayerPos();
                _self._thickBeam.Radius = _self.ThickBeamRadius;
                _self._thickBeam.MaxLength = 0;
            }

            public override void OnStateExited()
            {
                _self._thickBeam.Visible = false;
                _self._thickBeam.DamageEnabled = false;
                _self._thickBeam.DamagedPlayer -= OnDealtDamageToPlayer;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _timer += delta;

                UpdateThickBeam(delta);

                if (_self.AllPowerOrbsBroken())
                {
                    ChangeState<Vulnerable>();
                    return;
                }

                if (_timer >= _self.ThickBeamDuration)
                {
                    ChangeState<Submerging>();
                    return;
                }
            }

            private void UpdateThickBeam(float delta)
            {
                if (_timer >= _self.ThickBeamStartDelay)
                {
                    _self._thickBeam.Visible = true;
                    _self._thickBeam.DamageEnabled = true;

                    _self._thickBeam.MaxLength += _self.ThickBeamElongateSpeed * delta;

                    _self._thickBeam.TargetPos = _self._thickBeam.TargetPos.MoveToward(
                        PlayerPos(),
                        _self.ThickBeamTargetMoveSpeed * delta
                    );
                }
            }

            private void OnDealtDamageToPlayer()
            {
                ChangeState<Laughing>();
            }

            private Vector3 PlayerPos()
            {
                return GetTree().FindNode<Player>()
                    .GlobalPosition
                    .Flattened();
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
                ChangeState<Submerging>();
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

        private partial class Submerging : SeamonsterBossState
        {
            private Transform3D _initialPos;
            private Transform3D _targetPos;

            private float _timer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _initialPos = _self.GlobalTransform;
                _targetPos = _self._currentSpawnPos.Translated(Vector3.Up * _self.SubmergeDepth);

                _self.HidePowerOrbs();
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                float t = Mathf.Min(_timer / _self.SubmergingDuration, 1);
                _self.GlobalTransform = _initialPos.InterpolateWith(_targetPos, t);

                if (_timer >= _self.SubmergingDuration)
                {
                    _self.RandomizeSpawnPoint();
                    ChangeState<Submerged>();
                }
            }
        }
    }
}