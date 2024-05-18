using System;
using Godot;

namespace FastDragon
{
    public partial class SeamonsterBoss : CharacterBody3D
    {
        [ExportCategory("Timings")]
        [Export] public float SubmergedDuration = 1;
        [Export] public float SubmergingDuration = 0.5f;
        [Export] public float SurfacingDuration = 0.5f;
        [Export] public float IdleDuration = 1f;

        [ExportCategory("Points")]
        [Export] public float SubmergeDepth = -14;
        [Export] public Node3D InitialSpawnPoint;
        [Export] public Node3D CameraFixPoint;
        [Export] public Node3D[] SpawnPoints = new Node3D[0];

        [ExportCategory("Attack Parameters")]
        [ExportGroup("Thick Beam")]
        [Export] public float ThickBeamRadius = 1;
        [Export] public float ThickBeamTargetMoveSpeed = 2.5f;
        [Export] public float ThickBeamStartDelay = 0.5f;

        private BreakableArea3D _weakPoint => GetNode<BreakableArea3D>("%WeakPoint");
        private ThickBeam _thickBeam => GetNode<ThickBeam>("%ThickBeam");

        private readonly StateMachine _stateMachine = new StateMachine(typeof(SeamonsterBossState));
        private readonly Random _rng = new Random();

        private Transform3D _currentSpawnPos;


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
            }

            public override void _PhysicsProcess(double deltaD)
            {
                _timer += (float)deltaD;

                float t = Mathf.Min(_timer / _self.SurfacingDuration, 1);
                _self.GlobalTransform = _initialPos.InterpolateWith(_self._currentSpawnPos, t);

                if (_timer >= _self.SurfacingDuration)
                {
                    _self.GlobalTransform = _self._currentSpawnPos;
                    ChangeState<Idle>();
                }

            }
        }

        private partial class Idle : SeamonsterBossState
        {
            private float _timer;
            private bool _damagedPlayer;

            public override void OnStateEntered()
            {
                _timer = 0;
                _self._weakPoint.Visible = true;
                _self._weakPoint.Disabled = false;
                _self._weakPoint.Broken += OnDamagedByPlayer;

                _damagedPlayer = false;
                _self._thickBeam.DamagedPlayer += OnDealtDamageToPlayer;
                _self._thickBeam.TargetPos = PlayerPos();
                _self._thickBeam.Radius = _self.ThickBeamRadius;
            }

            public override void OnStateExited()
            {
                _self._weakPoint.Disabled = true;
                _self._weakPoint.Broken -= OnDamagedByPlayer;

                _self._thickBeam.Visible = false;
                _self._thickBeam.DamageEnabled = false;
                _self._thickBeam.DamagedPlayer -= OnDealtDamageToPlayer;
            }

            public override void _PhysicsProcess(double deltaD)
            {
                float delta = (float)deltaD;

                _timer += delta;

                if (_timer >= _self.ThickBeamStartDelay && !_damagedPlayer)
                {
                    _self._thickBeam.Visible = true;
                    _self._thickBeam.DamageEnabled = true;
                    _self._thickBeam.TargetPos = _self._thickBeam.TargetPos.MoveToward(
                        PlayerPos(),
                        _self.ThickBeamTargetMoveSpeed * delta
                    );
                }

                if (_timer >= _self.IdleDuration)
                    ChangeState<Submerging>();
            }

            private void OnDamagedByPlayer()
            {
                _self._weakPoint.Visible = false;
                ChangeState<Submerging>();
            }

            private void OnDealtDamageToPlayer()
            {
                _damagedPlayer = true;

                _self._thickBeam.Visible = false;
                _self._thickBeam.DamageEnabled = false;
            }

            private Vector3 PlayerPos()
            {
                return GetTree().FindNode<Player>()
                    .GlobalPosition
                    .Flattened();
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